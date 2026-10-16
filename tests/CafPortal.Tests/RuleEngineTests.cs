using System.Globalization;
using CafPortal.Application.Common;
using CafPortal.Domain.Enums;
using Xunit;

// Rule snapshots (KeywordRules / StatusRules) are process-wide statics; run serially so Load() can't race.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CafPortal.Tests;

public class KeywordRulesTests
{
    [Theory]
    [InlineData("1 - Validating & Initial Scope", 1)]
    [InlineData("2 - Executing Pre-Requisites", 2)]
    [InlineData("3 - Finalize Scope", 3)]
    [InlineData("4 - Executing Migration", 4)]
    [InlineData("Something else", null)]
    [InlineData(null, null)]
    public void Stage_maps_fdo_migration_status(string? text, int? expected)
        => Assert.Equal(expected, NominationRules.StageIndex(text));

    [Theory]
    [InlineData("Validating Commitment & Initial Scope", 1)]
    [InlineData("Executing Migration", 4)]
    [InlineData("5 - Deferred By Customer", 5)]
    [InlineData("6 - Cancelled / Archived", 6)]
    [InlineData("7-Completed", 7)]
    [InlineData("Completed", 7)]
    [InlineData("Something else", null)]
    public void FdoStage_maps_full_1_to_7(string text, int? expected)
    {
        Assert.Equal(expected, NominationRules.FdoStage(text));
        // Journey stage stays 1–4 only, so SLA/velocity are unaffected by settled stages.
        if (expected > 4) Assert.Null(NominationRules.StageIndex(text));
    }

    [Theory]
    [InlineData(null, "Completed", "Approved")]
    [InlineData("", "7-Completed", "Approved")]
    [InlineData(null, "Executing Migration", null)]
    [InlineData("Declined", "Completed", "Declined")]
    public void Blank_approval_on_completed_wave_reads_approved(string? approval, string migration, string? expected)
        => Assert.Equal(expected, NominationRules.EffectiveApproval(approval, migration));

    [Theory]
    [InlineData("Blocked - Customer", "Blocked")]
    [InlineData("Waiting for customer", "Waiting")]
    [InlineData("Follow-up needed", "Waiting")]
    [InlineData("On Track", "On Track")]
    [InlineData("", "Other")]
    public void Health_first_match_with_fallback(string text, string expected)
        => Assert.Equal(expected, NominationRules.HealthBucket(text));

    [Theory]
    [InlineData("GHCP", "GHCP-CI")]
    [InlineData("  GHCP   -  IAC ", "GHCP-CI")]
    [InlineData("GHCP CI SKILLS", "GHCP-CI")]
    [InlineData("GHCP CLI", "AppMod CLI")]
    [InlineData("GHCP App Modernization", "AppMod")]
    [InlineData("AppCat Analysis", "AppCAT")]
    [InlineData("Azure Migrate", "Azure Migrate")]
    public void Tool_alias_is_exact_and_whitespace_insensitive(string raw, string expected)
        => Assert.Equal(expected, ToolNameNormalizer.Canonical(raw));

    [Fact]
    public void Activity_collects_all_matches_and_falls_back()
    {
        var hits = ToolActivityClassifier.Classify("GHCP", "assessment then containerize and build CD pipeline");
        Assert.Contains("Application Assessment", hits);
        Assert.Contains("Containerization", hits);
        Assert.Contains("CD Pipeline", hits);
        Assert.Equal(new[] { "Unclassified" }, ToolActivityClassifier.Classify("GHCP", "general help"));
    }

    [Theory]
    [InlineData("Waiting on customer RBAC access", "Access Pending")]
    [InlineData("ALZ not ready", "Landing Zone Pending")]
    [InlineData("Pending CAB sign-off", "Approval Pending")]
    [InlineData("UAT in progress", "Testing/Validation Pending")]
    [InlineData("Customer is awaiting internal budget", "Budget/Priority Hold")] // budget outranks customer
    [InlineData("All good", null)]
    public void Blocker_reason_suggestion(string text, string? expected)
        => Assert.Equal(expected, KeywordRules.First(KeywordRules.BlockerReason, text));

    [Fact]
    public void Evaluate_respects_priority_for_first_match_domains()
    {
        var rules = new[]
        {
            new KeywordRuleItem(KeywordRules.Health, "track", "Contains", "On Track", 20),
            new KeywordRuleItem(KeywordRules.Health, "off track", "Contains", "Blocked", 10),
        };
        Assert.Equal(new[] { "Blocked" }, KeywordRules.Evaluate(rules, KeywordRules.Health, "Off track"));
    }

    [Fact]
    public void Load_replaces_the_snapshot()
    {
        try
        {
            KeywordRules.Load(new[] { new KeywordRuleItem(KeywordRules.Stage, "kickoff", "Contains", "1", 10) });
            Assert.Equal(1, NominationRules.StageIndex("Kickoff call"));
            Assert.Null(NominationRules.StageIndex("4 - Executing Migration"));
        }
        finally
        {
            KeywordRules.Load(KeywordRules.Defaults);
        }
    }
}

public class StatusRulesTests
{
    [Fact]
    public void Default_settled_set()
    {
        var settled = NominationRules.SettledStatuses.OrderBy(s => s).ToArray();
        Assert.Equal(new[] { NominationStatusType.CustomerDeferred, NominationStatusType.Completed,
            NominationStatusType.Closed, NominationStatusType.Withdrawn }.OrderBy(s => s), settled);
        Assert.True(StatusRules.IsBlocked(NominationStatusType.WaitingForCustomerAction));
        Assert.False(StatusRules.IsBlocked(NominationStatusType.InProgress));
    }

    [Fact]
    public void Load_changes_settled_behaviour()
    {
        try
        {
            StatusRules.Load(StatusRules.Defaults.Select(r =>
                r.Status == NominationStatusType.Closed ? r with { IsSettled = false } : r));
            Assert.False(NominationRules.IsSettled(NominationStatusType.Closed));
        }
        finally
        {
            StatusRules.Load(StatusRules.Defaults);
        }
    }
}

public class NominationRulesTests
{
    private static DateOnly D(string s) => DateOnly.Parse(s, CultureInfo.InvariantCulture);

    [Fact]
    public void Fiscal_year_anchors_on_close_date_when_settled_else_entry()
    {
        // Nominated FY26, closed FY27.
        Assert.Equal(2027, NominationRules.FiscalYearOf(true, D("2026-08-01"), D("2026-03-01"), D("2026-01-01")));
        Assert.Equal(2026, NominationRules.FiscalYearOf(false, D("2026-08-01"), D("2026-03-01"), D("2026-01-01")));
        // Fallbacks: no end date → nominated → opened.
        Assert.Equal(2026, NominationRules.FiscalYearOf(true, null, D("2026-03-01"), D("2025-01-01")));
        Assert.Equal(2027, NominationRules.FiscalYearOf(false, null, null, D("2026-07-01")));
    }

    [Theory]
    [InlineData(2, false, 12, 10, "")]        // over target by 2 → below Warn (3)
    [InlineData(2, false, 13, 10, "Warn")]    // target 10 + 3
    [InlineData(3, false, 10, 5, "Escalate")] // target 5 + 5
    [InlineData(4, false, 33, 23, "Defer")]   // target 23 + 10
    [InlineData(4, false, 20, 23, "")]        // within target
    [InlineData(1, false, 90, 10, "")]        // Stage 1 never tiered
    [InlineData(3, true, 90, 5, "")]          // settled never tiered
    public void Sla_tier_counts_days_past_stage_target(int stage, bool settled, int days, int target, string expected)
        => Assert.Equal(expected, NominationRules.StaleTier(stage, settled, days, target, 3, 5, 10));

    [Fact]
    public void Velocity_uses_fdo_total_days_against_total_allocation()
    {
        var s = new Dictionary<string, double> { ["StageTargetDays1"] = 10, ["StageTargetDays2"] = 10, ["StageTargetDays3"] = 5, ["StageTargetDays4"] = 25 };
        Assert.Equal((14, 50), NominationRules.Velocity(new int?[] { 10, 4, 0, 0 }, 4, s, fdoTotalDays: 14));
        Assert.Equal((63, 50), NominationRules.Velocity(new int?[] { 10, 17, 10, 26 }, 4, s));       // stage sum fallback
        Assert.Equal((27, 20), NominationRules.Velocity(new int?[] { 8, 19, null, null }, 2, s));    // in-flight: stages 1..2
        Assert.Equal((11, 13), NominationRules.Velocity(new int?[] { 8, 3, null, null }, 2, s, inFlight: true));   // stage 2 only 3 days in
        Assert.Equal((27, 20), NominationRules.Velocity(new int?[] { 8, 19, null, null }, 2, s, inFlight: true));  // over target: capped at target
        Assert.Null(NominationRules.Velocity(new int?[4], 4, s));
    }

    [Fact]
    public void Fdo_days_flagged_when_far_from_business_days()
    {
        Assert.Equal(5, NominationRules.BusinessDays(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 12)));   // Mon → next Mon
        var s = new Dictionary<string, double> { ["FdoDaysGapPct"] = 50 };
        var start = new DateOnly(2026, 4, 1);
        var end = new DateOnly(2026, 10, 1);
        var expected = NominationRules.BusinessDays(start, end);
        Assert.Equal(expected, NominationRules.FdoDaysExpected(14, start, end, s));        // Petroliam-style gap
        Assert.Null(NominationRules.FdoDaysExpected(expected - 5, start, end, s));          // close enough
        Assert.Null(NominationRules.FdoDaysExpected(14, null, end, s));                     // no start date: can't check
    }

    [Theory]
    [InlineData("Blocked", "danger")]
    [InlineData("Waiting", "warning")]
    [InlineData("On Track", "success")]
    [InlineData("Other", "informative")]
    public void Health_tone_maps_bucket(string bucket, string expected) => Assert.Equal(expected, NominationRules.HealthTone(bucket));

    [Theory]
    [InlineData("Warn", "warning")]
    [InlineData("Escalate", "severe")]
    [InlineData("Defer", "danger")]
    [InlineData("", null)]
    public void Sla_tone_maps_tier(string tier, string? expected) => Assert.Equal(expected, NominationRules.SlaTone(tier));

    [Fact]
    public void Data_quality_flags_cover_fdo_days_and_missing_cores()
    {
        Assert.Equal(new[] { "Check FDO days", "Missing cores" },
            NominationRules.DataQualityFlags(true, "Containerize to AKS", 0, CafPortal.Domain.Enums.NominationStatusType.InProgress));
        Assert.Empty(NominationRules.DataQualityFlags(false, "Containerize to AKS", 8, CafPortal.Domain.Enums.NominationStatusType.InProgress));
        Assert.Empty(NominationRules.DataQualityFlags(false, ".NET / Java apps migrations to PaaS", 0, CafPortal.Domain.Enums.NominationStatusType.InProgress));
        Assert.Empty(NominationRules.DataQualityFlags(false, "Containerize to AKS", 0, CafPortal.Domain.Enums.NominationStatusType.Withdrawn));
    }

    [Fact]
    public void Blocker_age_tone_uses_settings()
    {
        var s = new Dictionary<string, double> { ["BlockerAgeAmberDays"] = 5, ["BlockerAgeRedDays"] = 10 };
        Assert.Equal("informative", NominationRules.BlockerAgeTone(4, s));
        Assert.Equal("warning", NominationRules.BlockerAgeTone(5, s));
        Assert.Equal("danger", NominationRules.BlockerAgeTone(10, s));
    }

    [Theory]
    [InlineData(null, "Strategic", "Standard Factory")]       // segment no longer implies a classification
    [InlineData("Strategic Pilot", "Strategic", "Strategic Pilot")] // manual classification wins
    [InlineData(null, "Upper Majors", "Standard Factory")]
    [InlineData("", null, "Standard Factory")]
    public void Classification_defaults_from_fdo_segment(string? manual, string? segment, string expected)
        => Assert.Equal(expected, NominationRules.ClassificationOrDefault(manual, segment));

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("standard factory", false)]
    [InlineData("Strategic Pilot", true)]
    public void Strategic_is_anything_but_standard(string? classification, bool expected)
        => Assert.Equal(expected, NominationRules.IsStrategic(classification));

    [Fact]
    public void Fiscal_calendar_boundaries()
    {
        Assert.Equal(2027, FiscalCalendar.FiscalYear(D("2026-07-01")));
        Assert.Equal(2026, FiscalCalendar.FiscalYear(D("2026-06-30")));
        Assert.Equal(1, FiscalCalendar.FiscalQuarter(D("2026-09-30")));
        Assert.Equal(2, FiscalCalendar.FiscalQuarter(D("2026-10-01")));
        Assert.Equal(4, FiscalCalendar.FiscalQuarter(D("2027-06-30")));
        Assert.Equal("FY27", FiscalCalendar.FyLabel(2027));
    }
}

public class ScoringTests
{
    private static Dictionary<string, double> Defaults() =>
        SettingsCatalog.All.ToDictionary(d => d.Key, d => double.Parse(d.Default, CultureInfo.InvariantCulture));

    private static readonly MsiModel Model = MsiModel.From(Defaults());

    [Fact]
    public void Catalog_defaults_are_valid() => Assert.Null(SettingsCatalog.Validate(Defaults()));

    [Fact]
    public void Validate_rejects_out_of_range_ordering_and_bad_weight_totals()
    {
        var v = Defaults(); v["StaleWarnDays"] = -1;
        Assert.NotNull(SettingsCatalog.Validate(v));

        v = Defaults(); v["StaleWarnDays"] = v["StaleEscalateDays"];
        Assert.NotNull(SettingsCatalog.Validate(v));

        v = Defaults(); v["MsiWeightReadiness"] += 5;
        Assert.Contains("total 100", SettingsCatalog.Validate(v));

        v = Defaults(); v["WorkloadStartLookaheadDays"] = 1.5;
        Assert.NotNull(SettingsCatalog.Validate(v));
    }

    [Theory]
    [InlineData(100, 100, 100, 100, 100, 100, 100, "Green")]
    [InlineData(80, 80, 80, 80, 80, 80, 80, "Amber")] // 80 is not strictly above 80
    [InlineData(60, 60, 60, 60, 60, 60, 60, "Amber")]
    [InlineData(59, 59, 59, 59, 59, 59, 59, "Red")]
    public void Msi_weighted_score_and_bands(int r, int s, int d, int risk, int g, int so, int score, string band)
    {
        var res = MsiCalculator.Compute(Model, r, s, d, risk, g, so);
        Assert.Equal(score, res.Score);
        Assert.Equal(band, res.Band);
    }

    [Fact]
    public void Msi_risk_and_ghcp_components()
    {
        Assert.Equal(100, MsiCalculator.RiskHealth(Model, 0, false));
        Assert.Equal(70, MsiCalculator.RiskHealth(Model, 1, false));
        Assert.Equal(50, MsiCalculator.RiskHealth(Model, 1, true));
        Assert.Equal(0, MsiCalculator.RiskHealth(Model, 5, true));
        Assert.Equal(0, MsiCalculator.GhcpScore(null));
        Assert.Equal(100, MsiCalculator.GhcpScore(7));
        Assert.Equal(100, MsiCalculator.GhcpScore(12));
        Assert.Equal("Readiness", MsiCalculator.NormalizeGroup("bogus"));
        Assert.Equal("Signoff", MsiCalculator.NormalizeGroup(" signoff "));
    }

    [Theory]
    [InlineData(null, null, new string[0], 0)]
    [InlineData(null, true, new string[0], 4)]
    [InlineData(null, false, new[] { "Documentation Generation" }, 5)]
    [InlineData(null, null, new[] { "Containerization" }, 6)]
    [InlineData(null, null, new[] { "Application Assessment", "Code Remediation" }, 7)]
    [InlineData(6, null, new[] { "Documentation Generation" }, 6)] // manual level never downgraded
    public void Effective_ghcp_level(int? stored, bool? attached, string[] activities, int expected)
        => Assert.Equal(expected, ToolActivityClassifier.EffectiveLevel(stored, attached, activities));
}

public class ImportProfilesTests
{
    private static HashSet<string> H(params string[] h) => new(h, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Detects_each_fdo_workbook_by_signature()
    {
        Assert.Equal("nominations", ImportProfiles.Detect(H("TPID", "Task Id", "Migration Status", "1 - Validating & Initial Scope"))?.Kind);
        Assert.Equal("toolusage", ImportProfiles.Detect(H("Account ID", "TPID", "Task ID", "Details", "Category", "Tool Name", "Created Date"))?.Kind);
        Assert.Equal("offerings", ImportProfiles.Detect(H("TPID", "Customer Name", "Factory Offering", "Total ACR"))?.Kind);
        // Shares TPID/Customer with Offerings but lacks Factory Offering → account master.
        Assert.Equal("accounts", ImportProfiles.Detect(H("Segment", "TPID", "Customer Name", "Account ID"))?.Kind);
        Assert.Null(ImportProfiles.Detect(H("Foo", "Bar")));
    }

    [Fact]
    public void Signature_clauses_and_alternatives()
    {
        Assert.True(ImportProfiles.Matches("A;B|C", H("A", "C")));
        Assert.False(ImportProfiles.Matches("A;B|C", H("A")));
        Assert.False(ImportProfiles.Matches("", H("A")));
    }

    [Fact]
    public void Column_candidates_come_from_snapshot_with_default_fallback()
    {
        Assert.Contains("Total Days", ImportProfiles.Candidates("nominations", "TotalDays"));
        try
        {
            ImportProfiles.Load(ImportProfiles.DefaultProfiles,
                new[] { new ImportColumnItem("nominations", "TotalDays", "Days Total|Total Business Days", 10) });
            Assert.Equal(new[] { "Days Total", "Total Business Days" }, ImportProfiles.Candidates("nominations", "TotalDays"));
            Assert.Contains("Migration Status", ImportProfiles.Candidates("nominations", "MigrationStatus")); // default fallback
        }
        finally
        {
            ImportProfiles.Load(ImportProfiles.DefaultProfiles, ImportProfiles.DefaultColumns);
        }
    }
}

public class NameMatchingTests
{
    [Fact]
    public void Similar_names_score_high_and_different_names_low()
    {
        Assert.True(NameMatching.Similarity("Viswanatha Selvam P", "Selvam P Viswanatha") >= 0.95);
        Assert.True(NameMatching.Similarity("Ravinder Rana", "Ravinder  Rana.") >= 0.95);
        Assert.True(NameMatching.Similarity("Ravinder Rana", "Sushil Gupta") < 0.82);
    }
}
