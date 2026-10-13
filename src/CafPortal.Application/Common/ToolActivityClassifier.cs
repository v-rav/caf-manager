namespace CafPortal.Application.Common;

/// <summary>Infers the migration activity from the free-text FDO tool-usage note (the export conflates tool +
/// activity in one column), and derives an effective GHCP adoption level (0–7) from reported usage.</summary>
public static class ToolActivityClassifier
{
    // Activity keywords are data: KeywordRules domain "Activity" (edited on the Classification Rules page).
    public static IReadOnlyList<string> Classify(string? toolName, string? details)
    {
        var hits = KeywordRules.All(KeywordRules.Activity, (toolName ?? string.Empty) + " " + (details ?? string.Empty));
        return hits.Count > 0 ? hits : new List<string> { KeywordRules.FindDomain(KeywordRules.Activity)!.Fallback! };
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
