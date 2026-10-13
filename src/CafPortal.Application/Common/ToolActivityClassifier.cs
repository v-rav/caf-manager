namespace CafPortal.Application.Common;

/// <summary>Infers the migration activity from the free-text FDO tool-usage note (the export conflates tool +
/// activity in one column), and derives an effective GHCP adoption level (0–7) from reported usage.</summary>
public static class ToolActivityClassifier
{
    // Keyword → canonical master activity. Order matters (more specific pipeline phrases before the generic one).
    private static readonly (string Keyword, string Activity)[] Keywords =
    {
        ("iac", "IaC Generation"),
        ("infrastructure as code", "IaC Generation"),
        ("cd pipeline", "CD Pipeline"),
        ("ci pipeline", "CI Pipeline"),
        ("ci/cd", "CI Pipeline"),
        ("pipeline", "CI Pipeline"),
        ("container", "Containerization"),
        ("remediat", "Code Remediation"),
        ("assess", "Application Assessment"),
        ("dependency", "Dependency Analysis"),
        ("upgrade", "Version Upgrade"),
        ("modern", "Modernization"),
        ("document", "Documentation Generation"),
        ("test", "Testing & Validation"),
    };

    public static IReadOnlyList<string> Classify(string? toolName, string? details)
    {
        var text = ((toolName ?? string.Empty) + " " + (details ?? string.Empty)).ToLowerInvariant();
        var hits = new List<string>();
        foreach (var (keyword, activity) in Keywords)
            if (text.Contains(keyword) && !hits.Contains(activity))
                hits.Add(activity);
        return hits.Count > 0 ? hits : new List<string> { "Unclassified" };
    }

    private static readonly HashSet<string> Assessment = new(StringComparer.OrdinalIgnoreCase) { "Application Assessment", "Dependency Analysis" };
    private static readonly HashSet<string> Execution = new(StringComparer.OrdinalIgnoreCase)
    { "Code Remediation", "Modernization", "Version Upgrade", "Containerization", "IaC Generation", "CI Pipeline", "CD Pipeline" };

    /// <summary>Effective GHCP adoption level (0–7). Levels 5–7 are evidenced by reported usage, 4 by tool-attached;
    /// a manually-set level is never downgraded (we take the max). 7 = both assessing and executing with GHCP.</summary>
    public static int EffectiveLevel(int? storedLevel, bool? toolAttached, IReadOnlyCollection<string> activities)
    {
        var derived = 0;
        if (activities.Count > 0)
        {
            var hasAssess = activities.Any(Assessment.Contains);
            var hasExec = activities.Any(Execution.Contains);
            derived = hasAssess && hasExec ? 7 : hasExec ? 6 : 5; // any reported usage implies at least 5
        }
        else if (toolAttached == true)
        {
            derived = 4;
        }
        return Math.Max(storedLevel ?? 0, derived);
    }
}
