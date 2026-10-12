using System.Globalization;
using System.Text;

namespace CafPortal.Application.Common;

/// <summary>
/// Deterministic fuzzy name matching used to surface inconsistent account/resource name variants.
/// No external/LLM calls — canonicalization + token-set + Levenshtein similarity, all offline and auditable.
/// Identity keys (TPID / PSID) are authoritative: the service never clusters two distinct keys.
/// </summary>
public static class NameMatching
{
    private static readonly char[] Separators =
        { ' ', '\t', '\n', '-', '_', '.', ',', '/', '\\', '&', '+', '(', ')', '\'', '"', ':', ';', '@' };

    // Dropped during canonicalization so corporate suffixes / connectors don't dominate a match.
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ltd", "limited", "inc", "incorporated", "corp", "corporation", "co", "company", "companies",
        "plc", "llc", "llp", "lp", "sa", "ag", "gmbh", "nv", "bv", "spa", "srl", "sas", "pte", "pty",
        "aps", "oyj", "ab", "asa", "kg", "group", "groupe", "holding", "holdings", "the", "and", "of",
        "global", "international", "intl", "worldwide", "services", "service", "solutions", "systems"
    };

    /// <summary>Lowercase, strip diacritics, drop punctuation + corporate stopwords, then sort tokens (order-independent).</summary>
    public static string Canonicalize(string? s)
    {
        var tokens = Tokenize(s);
        tokens.Sort(StringComparer.Ordinal);
        return string.Join(' ', tokens);
    }

    /// <summary>Ordered, stopword-stripped, concatenated form used for edit-distance (keeps original word order).</summary>
    public static string Sequence(string? s) => string.Concat(Tokenize(s));

    private static List<string> Tokenize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return new List<string>();
        var folded = StripDiacritics(s).ToLowerInvariant();
        return folded
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => new string(t.Where(char.IsLetterOrDigit).ToArray()))
            .Where(t => t.Length > 0 && !Stopwords.Contains(t))
            .ToList();
    }

    /// <summary>Unicode fold: Société → Societe, Nestlé → Nestle.</summary>
    public static string StripDiacritics(string s)
    {
        var decomposed = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>0..1 similarity of two raw names.</summary>
    public static double Similarity(string? a, string? b)
        => Score(Canonicalize(a), Sequence(a), Canonicalize(b), Sequence(b));

    /// <summary>
    /// 0..1 similarity from precomputed forms (avoids recomputing in O(n²) loops): max of a token-set ratio
    /// (reorder / multi-word subset) and a Levenshtein ratio on the ordered sequence (typos).
    /// </summary>
    public static double Score(string canonSortedA, string seqA, string canonSortedB, string seqB)
    {
        if (canonSortedA.Length == 0 || canonSortedB.Length == 0) return 0;
        if (canonSortedA == canonSortedB) return 1.0;
        var ts = TokenSetRatio(canonSortedA, canonSortedB);
        var lev = LevenshteinRatio(seqA, seqB);
        return Math.Max(ts, lev);
    }

    private static double TokenSetRatio(string canonSortedA, string canonSortedB)
    {
        var sa = canonSortedA.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var sb = canonSortedB.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (sa.Count == 0 || sb.Count == 0) return 0;
        var intersect = sa.Count(sb.Contains);
        if (intersect == 0) return 0;
        var union = sa.Count + sb.Count - intersect;
        var jaccard = (double)intersect / union;
        var minCount = Math.Min(sa.Count, sb.Count);
        // Reward a subset only for a genuine multi-token overlap (≥2 shared) — never a single shared generic
        // token ("bank", "energy", a shared first name), which otherwise over-clusters unrelated entities.
        var containment = minCount >= 2 && intersect >= 2 ? (double)intersect / minCount * 0.94 : 0;
        return Math.Max(jaccard, containment);
    }

    /// <summary>Classic Levenshtein edit distance (two-row DP).</summary>
    public static int Levenshtein(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }

    /// <summary>Levenshtein as a 0..1 ratio (1 = identical). No prefix bias, so unrelated same-prefix names score low.</summary>
    public static double LevenshteinRatio(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0;
        if (a == b) return 1.0;
        var dist = Levenshtein(a, b);
        return 1.0 - (double)dist / Math.Max(a.Length, b.Length);
    }
}
