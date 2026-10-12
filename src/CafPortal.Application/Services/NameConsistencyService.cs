using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>
/// Deterministic fuzzy name-consistency assist. Clusters likely-same names via union-find over
/// <see cref="NameMatching"/> similarity, and applies approved variants as aliases. No LLM / external calls.
/// </summary>
public class NameConsistencyService(IApplicationDbContext db) : INameConsistencyService
{
    private const int MaxGroups = 250;

    public async Task<NameConsistencyReportDto> GetAccountSuggestionsAsync(double threshold, CancellationToken ct = default)
    {
        threshold = Clamp(threshold, 0.80);
        var accounts = await db.Accounts.AsNoTracking()
            .Select(a => new { a.AccountId, a.AccountName, a.Tpid })
            .ToListAsync(ct);
        var linkCounts = await db.Nominations.AsNoTracking()
            .Where(n => n.AccountId != null)
            .GroupBy(n => n.AccountId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var nodes = accounts.Select(a => new Node(
            a.AccountId, a.AccountName ?? string.Empty, a.Tpid,
            NameMatching.Canonicalize(a.AccountName), NameMatching.Sequence(a.AccountName),
            linkCounts.TryGetValue(a.AccountId, out var c) ? c : 0)).ToList();

        // Canonical preference: has TPID, then most linked, then longest name.
        var groups = Cluster(nodes, threshold,
            canonicalRank: n => (n.Tpid is { Length: > 0 } ? 1 : 0, n.Linked, n.Name.Length));

        // Drop variants already registered as an alias of their canonical (so applied ones don't resurface).
        var aliasPairs = await db.AccountAliases.AsNoTracking()
            .Select(x => new { x.Alias, x.StandardAccountName }).ToListAsync(ct);
        var known = new HashSet<string>(aliasPairs.Select(p => AliasKey(p.Alias, p.StandardAccountName)), StringComparer.OrdinalIgnoreCase);
        groups = RemoveApplied(groups, (g, v) => known.Contains(AliasKey(v.Name, g.CanonicalName)));

        return new NameConsistencyReportDto("accounts", threshold, groups.Count, groups.Sum(g => g.Variants.Count), groups);
    }

    public async Task<NameConsistencyReportDto> GetResourceSuggestionsAsync(double threshold, CancellationToken ct = default)
    {
        threshold = Clamp(threshold, 0.80);
        var resources = await db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Name, r.Psid })
            .ToListAsync(ct);
        var linkCounts = await db.ResourceAccounts.AsNoTracking()
            .GroupBy(ra => ra.ResourceId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var nodes = resources.Select(r => new Node(
            r.ResourceId, r.Name ?? string.Empty, r.Psid,
            NameMatching.Canonicalize(r.Name), NameMatching.Sequence(r.Name),
            linkCounts.TryGetValue(r.ResourceId, out var c) ? c : 0)).ToList();

        // Canonical preference: has PSID, then most linked, then longest name.
        var groups = Cluster(nodes, threshold,
            canonicalRank: n => (n.Tpid is { Length: > 0 } ? 1 : 0, n.Linked, n.Name.Length));

        // Drop variants already present on the canonical resource's alias list.
        var aliasLists = await db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Aliases }).ToListAsync(ct);
        var aliasByRes = aliasLists.ToDictionary(r => r.ResourceId, r => SplitAliases(r.Aliases));
        groups = RemoveApplied(groups, (g, v) =>
            aliasByRes.TryGetValue(g.CanonicalId, out var list) && list.Contains(v.Name, StringComparer.OrdinalIgnoreCase));

        return new NameConsistencyReportDto("resources", threshold, groups.Count, groups.Sum(g => g.Variants.Count), groups);
    }

    public async Task<NameConsistencyApplyResultDto> ApplyAccountAliasesAsync(ApplyAccountAliasRequest request, CancellationToken ct = default)
    {
        var canonical = (request.CanonicalName ?? string.Empty).Trim();
        if (canonical.Length == 0) return new NameConsistencyApplyResultDto(0, "Canonical name is required.");

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.AccountName == canonical, ct);
        var existingAliases = await db.AccountAliases.Select(x => x.Alias).ToListAsync(ct);
        var known = new HashSet<string>(existingAliases, StringComparer.OrdinalIgnoreCase);

        var written = 0;
        var accountAliasList = SplitAliases(account?.Aliases);
        foreach (var raw in request.Aliases ?? Array.Empty<string>())
        {
            var variant = (raw ?? string.Empty).Trim();
            if (variant.Length == 0 || string.Equals(variant, canonical, StringComparison.OrdinalIgnoreCase)) continue;
            if (!known.Contains(variant))
            {
                db.AccountAliases.Add(new AccountAlias { Alias = variant, StandardAccountName = canonical });
                known.Add(variant);
                written++;
            }
            if (!accountAliasList.Contains(variant, StringComparer.OrdinalIgnoreCase))
                accountAliasList.Add(variant);
        }
        if (account is not null)
            account.Aliases = accountAliasList.Count > 0 ? string.Join("; ", accountAliasList) : account.Aliases;

        await db.SaveChangesAsync(ct);
        return new NameConsistencyApplyResultDto(written,
            written > 0 ? $"Registered {written} alias(es) for \"{canonical}\"." : "No new aliases to register.");
    }

    public async Task<NameConsistencyApplyResultDto> ApplyResourceAliasesAsync(ApplyResourceAliasRequest request, CancellationToken ct = default)
    {
        var resource = await db.Resources.FirstOrDefaultAsync(r => r.ResourceId == request.ResourceId, ct);
        if (resource is null) return new NameConsistencyApplyResultDto(0, "Resource not found.");

        var list = SplitAliases(resource.Aliases);
        var written = 0;
        foreach (var raw in request.Aliases ?? Array.Empty<string>())
        {
            var variant = (raw ?? string.Empty).Trim();
            if (variant.Length == 0 || string.Equals(variant, resource.Name, StringComparison.OrdinalIgnoreCase)) continue;
            if (!list.Contains(variant, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(variant);
                written++;
            }
        }
        resource.Aliases = list.Count > 0 ? string.Join("; ", list) : resource.Aliases;
        await db.SaveChangesAsync(ct);
        return new NameConsistencyApplyResultDto(written,
            written > 0 ? $"Added {written} alias(es) to \"{resource.Name}\"." : "No new aliases to add.");
    }

    // ---- clustering ----

    private sealed record Node(int Id, string Name, string? Tpid, string Canon, string Seq, int Linked);

    /// <summary>Two rows with distinct identity keys (TPID / PSID) are confirmed-different entities, never aliases.</summary>
    private static bool DistinctIdentity(Node a, Node b) =>
        a.Tpid is { Length: > 0 } ta && b.Tpid is { Length: > 0 } tb &&
        !string.Equals(ta, tb, StringComparison.OrdinalIgnoreCase);

    private static List<NameSuggestionDto> Cluster(
        List<Node> nodes, double threshold, Func<Node, (int, int, int)> canonicalRank)
    {
        var n = nodes.Count;
        var parent = new int[n];
        for (var i = 0; i < n; i++) parent[i] = i;

        int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
        void Union(int a, int b) { var ra = Find(a); var rb = Find(b); if (ra != rb) parent[ra] = rb; }

        for (var i = 0; i < n; i++)
        {
            if (nodes[i].Canon.Length == 0) continue;
            for (var j = i + 1; j < n; j++)
            {
                if (nodes[j].Canon.Length == 0) continue;
                if (DistinctIdentity(nodes[i], nodes[j])) continue; // different TPID/PSID = different entity
                if (NameMatching.Score(nodes[i].Canon, nodes[i].Seq, nodes[j].Canon, nodes[j].Seq) >= threshold)
                    Union(i, j);
            }
        }

        var components = new Dictionary<int, List<int>>();
        for (var i = 0; i < n; i++)
        {
            if (nodes[i].Canon.Length == 0) continue;
            var root = Find(i);
            (components.TryGetValue(root, out var list) ? list : components[root] = new List<int>()).Add(i);
        }

        var result = new List<NameSuggestionDto>();
        foreach (var members in components.Values)
        {
            if (members.Count < 2) continue;
            var canonicalIdx = members.OrderByDescending(i => canonicalRank(nodes[i])).First();
            var canon = nodes[canonicalIdx];
            var variants = members.Where(i => i != canonicalIdx)
                // A member with its own distinct identity key is a real separate record, not an alias.
                .Where(i => !DistinctIdentity(nodes[i], canon))
                .Select(i => new NameVariantDto(
                    nodes[i].Id, nodes[i].Name, nodes[i].Tpid,
                    Math.Round(NameMatching.Score(nodes[i].Canon, nodes[i].Seq, canon.Canon, canon.Seq), 3),
                    nodes[i].Linked))
                .OrderByDescending(v => v.Score)
                .ToList();
            if (variants.Count == 0) continue;
            result.Add(new NameSuggestionDto(
                canon.Id, canon.Name, canon.Tpid, variants.Min(v => v.Score), variants));
        }

        return result.OrderByDescending(g => g.Variants.Count).ThenByDescending(g => g.MinScore).Take(MaxGroups).ToList();
    }

    private static List<string> SplitAliases(string? aliases) =>
        string.IsNullOrWhiteSpace(aliases)
            ? new List<string>()
            : aliases.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string AliasKey(string? alias, string? standard) =>
        $"{(alias ?? string.Empty).Trim()}\u0001{(standard ?? string.Empty).Trim()}";

    private static List<NameSuggestionDto> RemoveApplied(
        List<NameSuggestionDto> groups, Func<NameSuggestionDto, NameVariantDto, bool> applied)
    {
        var result = new List<NameSuggestionDto>();
        foreach (var g in groups)
        {
            var kept = g.Variants.Where(v => !applied(g, v)).ToList();
            if (kept.Count == 0) continue;
            result.Add(g with { Variants = kept, MinScore = kept.Min(v => v.Score) });
        }
        return result;
    }

    private static double Clamp(double v, double min) => v < min ? min : (v > 0.999 ? 0.999 : v);
}
