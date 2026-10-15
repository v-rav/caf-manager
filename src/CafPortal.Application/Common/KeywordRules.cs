using System.Text.RegularExpressions;

namespace CafPortal.Application.Common;

/// <summary>One active classification rule (snapshot of a <c>KeywordRule</c> row).</summary>
public sealed record KeywordRuleItem(string Domain, string Pattern, string MatchType, string Target, int Priority);

/// <summary>A rule domain: what it classifies, how it evaluates, and which targets are allowed.</summary>
public sealed record RuleDomain(string Key, string Label, string Source, bool AllMatches, string? Fallback, string[]? FixedTargets, string Description);

/// <summary>Data-driven keyword classification. Rules live in the <c>KeywordRules</c> table (edited on the Classification
/// Rules page); this holds the loaded snapshot and evaluates it. <see cref="Defaults"/> seeds an empty table and is the
/// fallback before the first load.</summary>
public static class KeywordRules
{
    public const string Stage = "Stage";
    public const string Health = "Health";
    public const string WaveType = "WaveType";
    public const string ToolAlias = "ToolAlias";
    public const string Activity = "Activity";
    public const string ContainerPath = "ContainerPath";
    public const string BlockerReason = "BlockerReason";
    public const string SegmentClass = "SegmentClass";

    public static readonly IReadOnlyList<RuleDomain> Domains = new RuleDomain[]
    {
        new(Stage, "Stage (FDO Migration Status → 1–4)", "Nomination Migration Status", false, null,
            new[] { "1", "2", "3", "4" }, "First matching rule sets the stage number shown everywhere."),
        new(Health, "Health (FDO Current State → bucket)", "Nomination Current State", false, "Other",
            new[] { "Blocked", "Waiting", "On Track" }, "First match wins; no match = Other. Drives status colours and health cuts."),
        new(WaveType, "Wave type (FDO 'Linked to' name → type)", "FDO wave link names", false, null,
            new[] { "Security", "Db", "LandingZone", "Dispatch" }, "Applied on the next Detail View import; no match = not tracked."),
        new(ToolAlias, "Tool alias (FDO tool name → master tool)", "Reported tool usage names", false, null,
            null, "Maps raw tool names to a Capability Master tool; unmapped names pass through as-is."),
        new(Activity, "Activity (usage notes → activities)", "Reported tool usage (name + details)", true, "Unclassified",
            null, "Every matching rule adds its activity; drives capability rollups and the effective GHCP level."),
        new(ContainerPath, "Containerized path (migration path)", "Nomination Primary Migration Path", false, null,
            new[] { "Containerized" }, "Any match marks the path containerized for ACR capture."),
        new(BlockerReason, "Blocker reason suggestion (remarks / state → reason)", "Nomination Remarks, else Current State", false, null,
            new[] { "Access Pending", "Landing Zone Pending", "Approval Pending", "Testing/Validation Pending", "Budget/Priority Hold",
                "Dependency Pending", "Internal Alignment", "Waiting for Customer Action" },
            "Pre-fills the blocker reason in the Manage dialog when a nomination is set to a blocked status. First match wins."),
        new(SegmentClass, "Default classification (FDO account segment → classification)", "Account Segment (FDO)", false, null,
            null, "Classification used when none is set on the nomination (a manual classification always wins). Drives the Strategic Register."),
    };

    public static RuleDomain? FindDomain(string key) =>
        Domains.FirstOrDefault(d => d.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    // Today's behaviour, captured as data. Order = priority.
    public static readonly IReadOnlyList<KeywordRuleItem> Defaults = Build(
        (Stage, "Contains", new[] { ("validating", "1"), ("pre-requisite", "2"), ("pre requisite", "2"), ("prerequisite", "2"),
            ("finalize", "3"), ("executing migration", "4") }),
        (Health, "Contains", new[] { ("block", "Blocked"), ("waiting", "Waiting"), ("follow", "Waiting"), ("on track", "On Track") }),
        (WaveType, "Contains", new[] { ("security", "Security"), ("sql", "Db"), ("ossdb", "Db"), ("database", "Db"),
            ("db migration", "Db"), ("landing zone", "LandingZone"), ("alz", "LandingZone"), ("dispatch", "Dispatch") }),
        (ToolAlias, "Exact", new[] { ("ghcp", "GHCP-CI"), ("ghcp-ci", "GHCP-CI"), ("ghcp - ci", "GHCP-CI"), ("ghcp ci", "GHCP-CI"),
            ("ghcp ci skills", "GHCP-CI"), ("ghcp-iac", "GHCP-CI"), ("ghcp - iac", "GHCP-CI"), ("ghcp iac", "GHCP-CI"),
            ("ghcp cli", "AppMod CLI"), ("ghcp app mod", "AppMod"), ("ghcp app modernization", "AppMod"),
            ("appcat analysis", "AppCAT"), ("appcat", "AppCAT") }),
        (Activity, "Contains", new[] { ("iac", "IaC Generation"), ("infrastructure as code", "IaC Generation"),
            ("cd pipeline", "CD Pipeline"), ("ci pipeline", "CI Pipeline"), ("ci/cd", "CI Pipeline"), ("pipeline", "CI Pipeline"),
            ("container", "Containerization"), ("remediat", "Code Remediation"), ("assess", "Application Assessment"),
            ("dependency", "Dependency Analysis"), ("upgrade", "Version Upgrade"), ("modern", "Modernization"),
            ("document", "Documentation Generation"), ("test", "Testing & Validation") }),
        (ContainerPath, "Contains", new[] { ("container", "Containerized"), ("aks", "Containerized"), ("aca", "Containerized"),
            ("eks", "Containerized"), ("ecs", "Containerized") }),
        (BlockerReason, "Contains", new[] { ("access", "Access Pending"), ("permission", "Access Pending"), ("rbac", "Access Pending"),
            ("landing zone", "Landing Zone Pending"), ("alz", "Landing Zone Pending"), ("prereq", "Landing Zone Pending"),
            ("pre-requisite", "Landing Zone Pending"), ("approv", "Approval Pending"), ("sign-off", "Approval Pending"),
            ("signoff", "Approval Pending"), ("test", "Testing/Validation Pending"), ("uat", "Testing/Validation Pending"),
            ("validation", "Testing/Validation Pending"), ("budget", "Budget/Priority Hold"), ("priorit", "Budget/Priority Hold"),
            ("cost", "Budget/Priority Hold"), ("depend", "Dependency Pending"), ("blocked by", "Dependency Pending"),
            ("align", "Internal Alignment"), ("internal", "Internal Alignment"), ("customer", "Waiting for Customer Action"),
            ("client", "Waiting for Customer Action"), ("awaiting", "Waiting for Customer Action") }),
        (SegmentClass, "Exact", new[] { ("Strategic", "Strategic Account") }));

    private static List<KeywordRuleItem> Build(params (string Domain, string Match, (string Pattern, string Target)[] Rules)[] sets) =>
        sets.SelectMany(s => s.Rules.Select((r, i) => new KeywordRuleItem(s.Domain, r.Pattern, s.Match, r.Target, (i + 1) * 10))).ToList();

    private static volatile IReadOnlyDictionary<string, KeywordRuleItem[]> _current = Index(Defaults);

    /// <summary>Replaces the active snapshot (called at startup and after every rule edit).</summary>
    public static void Load(IEnumerable<KeywordRuleItem> rules) => _current = Index(rules);

    private static IReadOnlyDictionary<string, KeywordRuleItem[]> Index(IEnumerable<KeywordRuleItem> rules) =>
        rules.GroupBy(r => r.Domain, StringComparer.OrdinalIgnoreCase)
             .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Priority).ToArray(), StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string s) => Regex.Replace(s.Trim().ToLowerInvariant(), @"\s+", " ");

    public static bool Matches(KeywordRuleItem rule, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(rule.Pattern)) return false;
        return rule.MatchType.Equals("Exact", StringComparison.OrdinalIgnoreCase)
            ? Normalize(text) == Normalize(rule.Pattern)
            : text.Contains(rule.Pattern.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Evaluates a domain against text with an explicit rule list (used for previews).</summary>
    public static IReadOnlyList<string> Evaluate(IEnumerable<KeywordRuleItem> rules, string domain, string? text)
    {
        var def = FindDomain(domain);
        var hits = new List<string>();
        foreach (var r in rules.Where(r => r.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase)).OrderBy(r => r.Priority))
        {
            if (!Matches(r, text ?? string.Empty) || hits.Contains(r.Target)) continue;
            hits.Add(r.Target);
            if (def is null || !def.AllMatches) break;
        }
        return hits;
    }

    public static IReadOnlyList<KeywordRuleItem> RulesFor(string domain) =>
        _current.TryGetValue(domain, out var r) ? r : Array.Empty<KeywordRuleItem>();

    /// <summary>First matching target for a first-match domain, or null.</summary>
    public static string? First(string domain, string? text) => Evaluate(RulesFor(domain), domain, text).FirstOrDefault();

    /// <summary>All matching targets for an all-match domain (empty when none).</summary>
    public static IReadOnlyList<string> All(string domain, string? text) => Evaluate(RulesFor(domain), domain, text);
}
