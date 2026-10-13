namespace CafPortal.Application.Common;

/// <summary>One editable operational setting: its key, UI grouping/label, seeded default, and validation bounds.</summary>
public sealed record SettingDefinition(
    string Key, string Group, string Label, string Default, double Min, double Max, bool Integer, string Description);

/// <summary>Catalog of every operational setting the portal reads. Single source for seeding, the Configuration UI,
/// and save-time validation — adding a rule-driving value means adding it here, not hardcoding it in a service.</summary>
public static class SettingsCatalog
{
    public static readonly IReadOnlyList<SettingDefinition> All = new SettingDefinition[]
    {
        new("StaleWarnDays", "SLA cadence", "Warn after (days)", "3", 1, 365, true,
            "Effective days in stage before a Stage 2–4 nomination is flagged Warn"),
        new("StaleEscalateDays", "SLA cadence", "Escalate after (days)", "5", 1, 365, true,
            "Effective days in stage before escalation"),
        new("StaleDeferDays", "SLA cadence", "Suggest Deferred after (days)", "10", 1, 365, true,
            "Effective days in stage before suggesting Customer Deferred"),

        new("StageTargetDays1", "Stage targets (informational)", "Stage 1 · Validating (days)", "10", 1, 365, true,
            "FDO target days for Stage 1 — not yet used by the SLA tier (PV-01)"),
        new("StageTargetDays2", "Stage targets (informational)", "Stage 2 · Pre-Requisites (days)", "10", 1, 365, true,
            "FDO target days for Stage 2 — not yet used by the SLA tier (PV-01)"),
        new("StageTargetDays3", "Stage targets (informational)", "Stage 3 · Finalize Scope (days)", "5", 1, 365, true,
            "FDO target days for Stage 3 — not yet used by the SLA tier (PV-01)"),
        new("StageTargetDays4", "Stage targets (informational)", "Stage 4 · Executing Migration (days)", "23", 1, 365, true,
            "FDO target days for Stage 4 — not yet used by the SLA tier (PV-01)"),

        new("StrategicGreenDays", "Strategic thresholds", "Green up to (days in flight)", "60", 1, 3650, true,
            "Strategic engagements stay Green up to this many days in flight"),
        new("StrategicAmberDays", "Strategic thresholds", "Amber up to (days in flight)", "90", 1, 3650, true,
            "Amber up to this many days; beyond it Red"),
        new("StrategicRedDays", "Strategic thresholds", "Red up to (days in flight)", "120", 1, 3650, true,
            "Red up to this many days; beyond it Exec escalation"),

        new("DefaultCapacityLimit", "Capacity", "Default capacity limit (accounts)", "5", 1, 50, true,
            "Limit used when neither role config nor a per-resource limit applies"),
        new("AvailableThreshold", "Capacity", "Available up to (% utilization)", "40", 1, 500, true,
            "Utilization % at or below which a resource is Available"),
        new("PartiallyUtilizedThreshold", "Capacity", "Partially utilized up to (%)", "80", 1, 500, true,
            "Utilization % at or below which a resource is Partially Utilized"),
        new("OverloadedThreshold", "Capacity", "Fully utilized up to (%)", "100", 1, 500, true,
            "Utilization % above which a resource is Overloaded"),

        new("LeaveClashWindowDays", "Resourcing", "Leave-clash window (days)", "30", 1, 365, true,
            "Upcoming-leave window used to detect coverage clashes"),
        new("PerformanceTrainingThreshold", "Resourcing", "Training-need score below", "4", 1, 5, false,
            "Review dimension score below which a training need is flagged"),

        new("HistoryRetentionDays", "Data retention", "Import history retention (days)", "90", 7, 3650, true,
            "Import run/change history older than this is pruned (nominations are never deleted)"),
    };

    private static readonly Dictionary<string, SettingDefinition> ByKey =
        All.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);

    public static SettingDefinition? Find(string key) => ByKey.GetValueOrDefault(key);

    // Ordered tiers that must stay ascending (strict where a band would otherwise collapse).
    private static readonly (string Lower, string Upper, bool Strict)[] Ordering =
    {
        ("StaleWarnDays", "StaleEscalateDays", true),
        ("StaleEscalateDays", "StaleDeferDays", true),
        ("StrategicGreenDays", "StrategicAmberDays", true),
        ("StrategicAmberDays", "StrategicRedDays", true),
        ("AvailableThreshold", "PartiallyUtilizedThreshold", true),
        ("PartiallyUtilizedThreshold", "OverloadedThreshold", false),
    };

    /// <summary>Validates a full candidate value set (current values merged with edits). Returns null when valid.</summary>
    public static string? Validate(IReadOnlyDictionary<string, double> values)
    {
        foreach (var (key, v) in values)
        {
            var def = Find(key);
            if (def is null) continue;
            if (v < def.Min || v > def.Max) return $"{def.Label} must be between {def.Min} and {def.Max}.";
            if (def.Integer && v != Math.Floor(v)) return $"{def.Label} must be a whole number.";
        }
        foreach (var (lower, upper, strict) in Ordering)
        {
            if (!values.TryGetValue(lower, out var lo) || !values.TryGetValue(upper, out var hi)) continue;
            if (strict ? lo >= hi : lo > hi)
                return $"{Find(lower)!.Label} must be {(strict ? "less than" : "at most")} {Find(upper)!.Label}.";
        }
        return null;
    }
}
