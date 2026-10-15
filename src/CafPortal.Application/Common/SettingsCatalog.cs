namespace CafPortal.Application.Common;

/// <summary>One editable setting: its key, UI section/group/label, seeded default, and validation bounds.</summary>
public sealed record SettingDefinition(
    string Key, string Group, string Label, string Default, double Min, double Max, bool Integer, string Description,
    string Section = "Operations");

/// <summary>Catalog of every operational setting the portal reads. Single source for seeding, the Configuration UI,
/// and save-time validation — adding a rule-driving value means adding it here, not hardcoding it in a service.</summary>
public static class SettingsCatalog
{
    public static readonly IReadOnlyList<SettingDefinition> All = new SettingDefinition[]
    {
        new("ClassificationEnabled", "Features (1 = on · 0 = off)", "Classification & Strategic Register", "0", 0, 1, true,
            "Off (parked): every nomination counts as Standard Factory — nothing is excluded from SLA/velocity and the Strategic Register is empty"),
        new("StaleWarnDays", "SLA cadence", "Warn after (days past target)", "3", 1, 365, true,
            "Days past the stage target before a Stage 2–4 nomination is flagged Warn"),
        new("StaleEscalateDays", "SLA cadence", "Escalate after (days past target)", "5", 1, 365, true,
            "Days past the stage target before escalation"),
        new("StaleDeferDays", "SLA cadence", "Suggest Deferred after (days past target)", "10", 1, 365, true,
            "Days past the stage target before suggesting Customer Deferred"),
        new("LongStalledDays", "SLA cadence", "Long-stalled after (days in stage)", "42", 1, 365, true,
            "Days in a Stage 2–4 after which the next step is 'move to Customer Deferred' (governance >6-week rule)"),

        new("StageTargetDays1", "Stage targets (SLA limit)", "Stage 1 · Validating (days)", "10", 1, 365, true,
            "Target days in Stage 1 (Stage 1 is not SLA-tiered)"),
        new("StageTargetDays2", "Stage targets (SLA limit)", "Stage 2 · Pre-Requisites (days)", "10", 1, 365, true,
            "Target days in Stage 2; the SLA cadence starts once this is exceeded"),
        new("StageTargetDays3", "Stage targets (SLA limit)", "Stage 3 · Finalize Scope (days)", "5", 1, 365, true,
            "Target days in Stage 3; the SLA cadence starts once this is exceeded"),
        new("StageTargetDays4", "Stage targets (SLA limit)", "Stage 4 · Executing Migration (days)", "23", 1, 365, true,
            "Target days in Stage 4; the SLA cadence starts once this is exceeded"),

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

        new("HeatAvailable", "Capacity heat colour (0 Green · 1 Amber · 2 Red)", "Available", "0", 0, 2, true,
            "Heat-map row colour for Available resources"),
        new("HeatPartiallyUtilized", "Capacity heat colour (0 Green · 1 Amber · 2 Red)", "Partially utilized", "1", 0, 2, true,
            "Heat-map row colour for Partially Utilized resources"),
        new("HeatFullyUtilized", "Capacity heat colour (0 Green · 1 Amber · 2 Red)", "Fully utilized", "1", 0, 2, true,
            "Heat-map row colour for Fully Utilized resources"),
        new("HeatOverloaded", "Capacity heat colour (0 Green · 1 Amber · 2 Red)", "Overloaded", "2", 0, 2, true,
            "Heat-map row colour for Overloaded resources"),

        new("BlockerAgeAmberDays", "Blocker ageing", "Amber from (days blocked)", "5", 1, 365, true,
            "Open blockers at or above this age show amber (Governance Board, Workspace)"),
        new("BlockerAgeRedDays", "Blocker ageing", "Red from (days blocked)", "10", 1, 365, true,
            "Open blockers at or above this age show red"),

        new("VelocityAmberBelowPct", "Velocity (target ÷ actual)", "Amber below (%)", "100", 10, 200, true,
            "Velocity below this % shows amber (100% = exactly on target; higher = faster)"),
        new("VelocityRedBelowPct", "Velocity (target ÷ actual)", "Red below (%)", "80", 10, 200, true,
            "Velocity below this % shows red"),

        new("LeaveClashWindowDays", "Resourcing", "Leave-clash window (days)", "30", 1, 365, true,
            "Upcoming-leave window used to detect coverage clashes"),
        new("WorkloadStartLookaheadDays", "Resourcing", "Count waves starting within (days)", "0", 0, 180, true,
            "A wave counts as active workload once its start (actual, else planned) is within this many days; later starts are Upcoming"),
        new("PerformanceTrainingThreshold", "Resourcing", "Training-need score below", "4", 1, 5, false,
            "Review dimension score below which a training need is flagged"),

        new("HistoryRetentionDays", "Data retention", "Import history retention (days)", "90", 7, 3650, true,
            "Import run/change history older than this is pruned (nominations are never deleted)"),

        // ---- Scoring models (section "Scoring") ----
        new("MsiWeightReadiness", "MSI weights (%)", "Readiness", "30", 0, 100, true, "Gates mapped to Readiness", "Scoring"),
        new("MsiWeightScope", "MSI weights (%)", "Scope", "20", 0, 100, true, "Gates mapped to Scope", "Scoring"),
        new("MsiWeightDelivery", "MSI weights (%)", "Delivery", "20", 0, 100, true, "Gates mapped to Delivery", "Scoring"),
        new("MsiWeightRisk", "MSI weights (%)", "Risk", "10", 0, 100, true, "Open blockers (see risk penalties)", "Scoring"),
        new("MsiWeightGhcp", "MSI weights (%)", "GHCP", "10", 0, 100, true, "Effective GHCP adoption level ÷ 7", "Scoring"),
        new("MsiWeightSignoff", "MSI weights (%)", "Sign-off", "10", 0, 100, true, "Gates mapped to Sign-off", "Scoring"),

        new("MsiGreenAbove", "MSI bands & risk", "Green when score above", "80", 1, 100, true, "MSI strictly above this is Green", "Scoring"),
        new("MsiAmberFrom", "MSI bands & risk", "Amber from score", "60", 0, 100, true, "MSI at or above this (and not Green) is Amber; below is Red", "Scoring"),
        new("MsiRiskPerBlocker", "MSI bands & risk", "Risk penalty per open blocker", "30", 0, 100, true, "Points removed from Risk health per open blocker", "Scoring"),
        new("MsiRiskClockStop", "MSI bands & risk", "Risk penalty when clock stopped", "20", 0, 100, true, "Extra points removed while any clock-stop blocker is open", "Scoring"),

        new("EffSaMsi", "Effectiveness · SA weights (%)", "Avg MSI", "30", 0, 100, true, "Quality-led SA score", "Scoring"),
        new("EffSaOnTrack", "Effectiveness · SA weights (%)", "On-track %", "25", 0, 100, true, "Active book without an SLA breach", "Scoring"),
        new("EffSaSpeed", "Effectiveness · SA weights (%)", "Delivery speed", "25", 0, 100, true, "From average delivery days", "Scoring"),
        new("EffSaMomentum", "Effectiveness · SA weights (%)", "Momentum", "20", 0, 100, true, "From average days-in-stage of the active book", "Scoring"),

        new("EffFlowOnTrack", "Effectiveness · PM/CFTL weights (%)", "On-track %", "30", 0, 100, true, "Flow-led PM/CFTL score", "Scoring"),
        new("EffFlowMomentum", "Effectiveness · PM/CFTL weights (%)", "Momentum", "30", 0, 100, true, "From average days-in-stage of the active book", "Scoring"),
        new("EffFlowSpeed", "Effectiveness · PM/CFTL weights (%)", "Delivery speed", "25", 0, 100, true, "From average delivery days", "Scoring"),
        new("EffFlowDelivered", "Effectiveness · PM/CFTL weights (%)", "Delivered share", "15", 0, 100, true, "Delivered ÷ assigned", "Scoring"),

        new("EffSpeedBestDays", "Effectiveness · targets & bands", "Speed = 100 at or under (days)", "45", 1, 3650, true, "Average delivery days scoring full marks", "Scoring"),
        new("EffSpeedWorstDays", "Effectiveness · targets & bands", "Speed = 0 at or over (days)", "180", 1, 3650, true, "Average delivery days scoring zero", "Scoring"),
        new("EffMomentumBestDays", "Effectiveness · targets & bands", "Momentum = 100 at or under (days)", "10", 1, 3650, true, "Average days-in-stage scoring full marks", "Scoring"),
        new("EffMomentumWorstDays", "Effectiveness · targets & bands", "Momentum = 0 at or over (days)", "75", 1, 3650, true, "Average days-in-stage scoring zero", "Scoring"),
        new("EffGreenFrom", "Effectiveness · targets & bands", "Green from score", "75", 1, 100, true, "Effectiveness at or above is Green", "Scoring"),
        new("EffAmberFrom", "Effectiveness · targets & bands", "Amber from score", "55", 0, 100, true, "Effectiveness at or above (and not Green) is Amber", "Scoring"),
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
        ("MsiAmberFrom", "MsiGreenAbove", true),
        ("BlockerAgeAmberDays", "BlockerAgeRedDays", true),
        ("VelocityRedBelowPct", "VelocityAmberBelowPct", true),
        ("EffSpeedBestDays", "EffSpeedWorstDays", true),
        ("EffMomentumBestDays", "EffMomentumWorstDays", true),
        ("EffAmberFrom", "EffGreenFrom", true),
    };

    // Weight groups whose members must total exactly 100%.
    private static readonly string[] SumTo100Groups =
        { "MSI weights (%)", "Effectiveness · SA weights (%)", "Effectiveness · PM/CFTL weights (%)" };

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
        foreach (var group in SumTo100Groups)
        {
            var members = All.Where(d => d.Group == group).ToList();
            if (!members.All(m => values.ContainsKey(m.Key))) continue;
            var total = members.Sum(m => values[m.Key]);
            if (Math.Abs(total - 100) > 0.001) return $"{group} must total 100 (currently {total}).";
        }
        return null;
    }

    /// <summary>Every catalog value (stored value, else its default) as numbers — the input to the scoring models.</summary>
    public static async Task<IReadOnlyDictionary<string, double>> ReadAsync(
        Abstractions.IApplicationDbContext db, CancellationToken ct = default)
    {
        var keys = All.Select(d => d.Key).ToList();
        var stored = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToDictionaryAsync(
            db.ApplicationSettings.Where(s => keys.Contains(s.Key)), s => s.Key, s => s.Value, ct);
        return All.ToDictionary(d => d.Key, d =>
            double.TryParse(stored.GetValueOrDefault(d.Key) ?? d.Default, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v)
                ? v : double.Parse(d.Default, System.Globalization.CultureInfo.InvariantCulture));
    }
}
