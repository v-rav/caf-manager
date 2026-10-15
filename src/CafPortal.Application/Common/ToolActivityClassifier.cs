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

    /// <summary>Effective GHCP adoption level (0–7). Levels 5–7 are evidenced by reported usage, 4 by tool-attached;
    /// a manually-set level is never downgraded (we take the max). 7 = both assessing and executing with GHCP.
    /// Which activities count as assessment vs execution is data: KeywordRules domain "ActivityPhase".</summary>
    public static int EffectiveLevel(int? storedLevel, bool? toolAttached, IReadOnlyCollection<string> activities)
    {
        var derived = 0;
        if (activities.Count > 0)
        {
            var phases = activities.Select(a => KeywordRules.First(KeywordRules.ActivityPhase, a)).ToHashSet();
            var hasAssess = phases.Contains("Assessment");
            var hasExec = phases.Contains("Execution");
            derived = hasAssess && hasExec ? 7 : hasExec ? 6 : 5; // any reported usage implies at least 5
        }
        else if (toolAttached == true)
        {
            derived = 4;
        }
        return Math.Max(storedLevel ?? 0, derived);
    }
}
