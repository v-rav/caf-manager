using CafPortal.Domain.Enums;

namespace CafPortal.Application.Common;

/// <summary>Single source of truth for nomination business rules shared by every service, export, import and page.
/// Computed here once and surfaced on <c>NominationDto</c> so the client never re-derives them.</summary>
public static class NominationRules
{
    /// <summary>States that stop a nomination counting as in-flight (capacity, SLA, KPIs). Data: StatusRules.</summary>
    public static NominationStatusType[] SettledStatuses => StatusRules.Settled;

    public static bool IsSettled(NominationStatusType status) => StatusRules.IsSettled(status);

    /// <summary>SLA tier for execution stages 2–4: the stage target (StageTargetDays1..4) is the limit, and the
    /// Warn/Escalate/Defer cadence counts days past that target. Stage 1 and settled work are never tiered.</summary>
    public static string StaleTier(int? stage, bool settled, int effectiveDays, int stageTarget, int warn, int escalate, int defer)
    {
        if (stage is null or < 2 || settled) return string.Empty;
        var over = effectiveDays - stageTarget;
        if (over >= defer) return "Defer";
        if (over >= escalate) return "Escalate";
        if (over >= warn) return "Warn";
        return string.Empty;
    }

    public static int StageTarget(IReadOnlyDictionary<string, double> settings, int? stage) =>
        stage is >= 1 and <= 4 ? (int)settings[$"StageTargetDays{stage}"] : 0;

    /// <summary>Long-stalled = SLA-tiered and at least LongStalledDays in the current stage.</summary>
    public static bool IsLongStalled(string staleTier, int days, IReadOnlyDictionary<string, double> settings) =>
        !string.IsNullOrEmpty(staleTier) && days >= (int)settings["LongStalledDays"];

    /// <summary>Governance cadence action for a stale Stage 2–4 wave (Day 3 / 5 / 10 past target, >6 weeks); null when none.</summary>
    public static string? NextStep(string staleTier, bool longStalled) =>
        longStalled ? "Delayed >6 weeks — move to Customer Deferred & set follow-up"
        : staleTier switch
        {
            "Warn" => "Send reminder, then move to Blocked",
            "Escalate" => "Send 2nd reminder / escalate",
            "Defer" => "Final reminder — move to Customer Deferred & set follow-up",
            _ => null,
        };

    /// <summary>Velocity inputs: (days spent, allocated target days) over stages 1..upToStage. <paramref name="fdoTotalDays"/>,
    /// when given, is FDO's own Total Days and replaces the stage sum as the days spent. <paramref name="inFlight"/>: the
    /// current stage is only partly elapsed, so it is allotted min(target, days so far) rather than its full target.</summary>
    public static (int Days, int Target)? Velocity(int?[] stageDays, int upToStage, IReadOnlyDictionary<string, double> settings,
        int? fdoTotalDays = null, bool inFlight = false)
    {
        if (upToStage is < 1 or > 4 || (fdoTotalDays is null && stageDays.All(d => d is null))) return null;
        var days = 0; var target = 0;
        for (var s = 1; s <= upToStage; s++)
        {
            var spent = stageDays[s - 1] ?? 0;
            days += spent;
            target += inFlight && s == upToStage ? Math.Min(StageTarget(settings, s), spent) : StageTarget(settings, s);
        }
        return target > 0 ? (fdoTotalDays ?? days, target) : null;
    }

    /// <summary>Badge tone for velocity (target ÷ actual, 1.0 = on target, higher = faster).</summary>
    public static string VelocityTone(double velocity, IReadOnlyDictionary<string, double> settings) =>
        velocity * 100 < settings["VelocityRedBelowPct"] ? "danger"
        : velocity * 100 < settings["VelocityAmberBelowPct"] ? "warning" : "success";

    /// <summary>Mon–Fri days from <paramref name="from"/> (inclusive) to <paramref name="to"/> (exclusive).</summary>
    public static int BusinessDays(DateOnly from, DateOnly to)
    {
        if (to <= from) return 0;
        var total = to.DayNumber - from.DayNumber;
        var weeks = total / 7;
        var days = weeks * 5;
        for (var d = from.AddDays(weeks * 7); d < to; d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) days++;
        return days;
    }

    /// <summary>Business days FDO should roughly report (actual start → actual end, else today) when FDO Total Days is off by
    /// more than FdoDaysGapPct; null when it looks consistent or can't be checked.</summary>
    public static int? FdoDaysExpected(int? fdoTotalDays, DateOnly? actualStart, DateOnly end, IReadOnlyDictionary<string, double> settings)
    {
        if (fdoTotalDays is null || actualStart is not DateOnly start) return null;
        var expected = BusinessDays(start, end);
        if (expected < 10) return null;
        var gap = Math.Abs(fdoTotalDays.Value - expected) * 100.0 / expected;
        return gap > settings["FdoDaysGapPct"] ? expected : null;
    }

    /// <summary>Badge tone for how long a blocker has been open (BlockerAgeAmberDays / BlockerAgeRedDays).</summary>
    public static string BlockerAgeTone(int daysBlocked, IReadOnlyDictionary<string, double> settings) =>
        daysBlocked >= settings["BlockerAgeRedDays"] ? "danger"
        : daysBlocked >= settings["BlockerAgeAmberDays"] ? "warning" : "informative";

    /// <summary>The one FY a nomination belongs to: settled by its close date, in-flight by when it entered the pipeline.</summary>
    public static int FiscalYearOf(bool settled, DateOnly? actualEnd, DateOnly? nominated, DateOnly opened) =>
        FiscalCalendar.FiscalYear(settled ? actualEnd ?? nominated ?? opened : nominated ?? opened);

    /// <summary>Latest effective start (Actual ?? Planned) that still counts as started workload: today + look-ahead.
    /// Later starts are pre-created future waves (Upcoming) and are excluded from workload, capacity, active KPIs and SLA.</summary>
    public static async Task<DateOnly> StartCutoffAsync(Abstractions.IApplicationDbContext db, CancellationToken ct = default) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays((int)(await SettingsCatalog.ReadAsync(db, ct))["WorkloadStartLookaheadDays"]);

    /// <summary>Classification that means "not strategic"; anything else is a strategic engagement.</summary>
    public const string StandardClassification = "Standard Factory";

    public static string ClassificationOrDefault(string? classification) =>
        string.IsNullOrWhiteSpace(classification) ? StandardClassification : classification!;

    /// <summary>Manual classification, else the SegmentClass rule for the FDO account segment, else Standard Factory.</summary>
    public static string ClassificationOrDefault(string? classification, string? segment) =>
        !string.IsNullOrWhiteSpace(classification) ? classification!
        : KeywordRules.First(KeywordRules.SegmentClass, segment) ?? StandardClassification;

    /// <summary>Feature flag (Configuration → Features): while off, classification is parked and everything is Standard.</summary>
    public static bool ClassificationEnabled(IReadOnlyDictionary<string, double> settings) =>
        settings.TryGetValue("ClassificationEnabled", out var v) && v >= 1;

    public static bool IsStrategic(string? classification) =>
        !ClassificationOrDefault(classification).Equals(StandardClassification, StringComparison.OrdinalIgnoreCase);

    public static readonly string[] StageLabels =
        { "Validating", "Executing Pre-Requisites", "Finalize Scope", "Executing Migration" };

    /// <summary>FDO Migration Status text → stage 1–4 (null when unrecognised). Keywords: KeywordRules "Stage".</summary>
    public static int? StageIndex(string? migration) =>
        int.TryParse(KeywordRules.First(KeywordRules.Stage, migration), out var stage) ? stage : null;

    public static string StageLabel(int? stage) =>
        stage is >= 1 and <= 4 ? StageLabels[stage.Value - 1] : string.Empty;

    /// <summary>FDO Current State text → health bucket. Keywords: KeywordRules "Health" (no match = Other).</summary>
    public static string HealthBucket(string? currentState) =>
        KeywordRules.First(KeywordRules.Health, currentState) ?? KeywordRules.FindDomain(KeywordRules.Health)!.Fallback!;
}
