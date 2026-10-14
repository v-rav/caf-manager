namespace CafPortal.Application.Common;

/// <summary>An FDO workbook type: how it is recognised (header signature) and in which order types are tried.
/// Signature = clauses separated by ';' that must ALL match; each clause lists alternative exact headers separated by '|'.</summary>
public sealed record ImportProfileItem(string Kind, string Label, int DetectOrder, string Signature);

/// <summary>Header candidates for one field of an import ('|'-separated; first header containing any candidate wins).</summary>
public sealed record ImportColumnItem(string Kind, string Field, string Candidates, int SortOrder);

/// <summary>Data-driven FDO import profiles (file detection + column header candidates). Rows live in the
/// <c>ImportProfiles</c> / <c>ImportColumnMaps</c> tables (Configuration → Import column maps); <see cref="Defaults"/>
/// seeds missing rows and is the fallback before the first load.</summary>
public static class ImportProfiles
{
    public const string Nominations = "nominations";
    public const string Offerings = "offerings";
    public const string Accounts = "accounts";
    public const string ToolUsage = "toolusage";

    public static readonly IReadOnlyList<ImportProfileItem> DefaultProfiles = new ImportProfileItem[]
    {
        new(ToolUsage, "Nomination Wize Tool Usage", 10, "Tool Name;Category;Created Date|Offering Name and Phase"),
        new(Offerings, "Summary of All Offerings", 20, "Factory Offering;Total ACR|Offering Id|Is Tool Attached"),
        new(Nominations, "Detail View (nomination pipeline)", 30, "1 - Validating & Initial Scope|Nomination Approval Status|Linked to ID"),
        new(Accounts, "Nominations In-Flight (account master)", 40, "Segment;TPID;Customer Name|Account ID"),
    };

    public static readonly IReadOnlyList<ImportColumnItem> DefaultColumns = Build(
        (Nominations, new[]
        {
            ("Tpid", "TPID|TP ID"), ("TaskId", "Task Id|TaskId"), ("AccountId", "Account ID|AccountId"),
            ("Customer", "Customer Name|Customer|Account Name"), ("Offering", "Offering Name|Offering|Wave"),
            ("Nominated", "Nominated date|Nominated|Wave Created"), ("MigrationStatus", "Migration Status"),
            ("Stage1Days", "Validating & Initial Scope|1 - Validating"), ("Stage2Days", "Executing Pre-Requisites|2 - Executing Pre"),
            ("Stage3Days", "Finalize Scope|3 - Finalize"), ("Stage4Days", "Executing Migration|4 - Executing Migration"),
            ("TotalDays", "Total Days"), ("Approval", "Approval Status|Nomination Approval"), ("Region", "Region|Area"),
            ("StatusSummary", "Status Summary"), ("NextAction", "Next Best Action"), ("CurrentState", "Current State"),
            ("SolutionArchitect", "Solution Architect Hierarchy - Solution Architect|Solution Architect"),
            ("Cftl", "CFTL Primary Hierarchy - CFTL Primary|CFTL Primary"),
            ("ProjectCoordinator", "Project Coordinator Hierarchy - Project Coordinator|Project Coordinator"),
            ("LinkedToId", "Linked to ID|Linked to Id"),
        }),
        (Offerings, new[]
        {
            ("Tpid", "TPID|TP ID"), ("TaskId", "Task ID|Task Id"), ("Segment", "Segment"),
            ("PrimaryMigrationPath", "Primary Migration Path"), ("PartnerName", "Partner Name"), ("TotalCores", "Total Cores"),
            ("IsToolAttached", "Is Tool Attached"), ("IsAutomationUsed", "Is Automation Used"), ("ModeOfAccess", "Mode Of Access"),
            ("TotalAcr", "Total ACR"), ("NnrAcr", "NNR ACR"),
            ("NominatedDate", "Nom. Created Date|Nomination Created Date|Account Nominated date"),
            ("ApprovalDate", "Nom. Approval Date|Nomination Approval Date"),
            ("ActualStartDate", "Actual Start Date"), ("ActualEndDate", "Actual End Date"),
            ("PlannedStartDate", "Planned Start Date"), ("PlannedEndDate", "Planned End Date"),
            ("Customer", "Customer Name|Customer"), ("Offering", "Factory Offering|Offering Name and Phase|Offering"),
            ("MigrationStatus", "Migration Status"), ("CurrentState", "Current State"), ("Region", "WW Region|Area|Region"),
        }),
        (Accounts, new[]
        {
            ("Segment", "Segment"), ("Tpid", "TPID|TP ID"), ("Customer", "Customer Name|Customer|Account Name"),
            ("AccountId", "Account ID|AccountId"),
        }),
        (ToolUsage, new[]
        {
            ("AccountId", "Account ID|AccountId"), ("Tpid", "TPID|TP ID"), ("TaskId", "Task ID|Task Id"),
            ("Details", "Details"), ("Category", "Category"), ("ToolName", "Tool Name"), ("CreatedDate", "Created Date"),
        }));

    private static List<ImportColumnItem> Build(params (string Kind, (string Field, string Candidates)[] Cols)[] sets) =>
        sets.SelectMany(s => s.Cols.Select((c, i) => new ImportColumnItem(s.Kind, c.Field, c.Candidates, (i + 1) * 10))).ToList();

    private static volatile IReadOnlyList<ImportProfileItem> _profiles = DefaultProfiles;
    private static volatile IReadOnlyDictionary<(string, string), string[]> _columns = Index(DefaultColumns);

    /// <summary>Replaces the active snapshot (startup and after every edit).</summary>
    public static void Load(IEnumerable<ImportProfileItem> profiles, IEnumerable<ImportColumnItem> columns)
    {
        _profiles = profiles.OrderBy(p => p.DetectOrder).ToList();
        _columns = Index(columns);
    }

    private static IReadOnlyDictionary<(string, string), string[]> Index(IEnumerable<ImportColumnItem> cols) =>
        cols.GroupBy(c => (c.Kind.ToLowerInvariant(), c.Field.ToLowerInvariant()))
            .ToDictionary(g => g.Key, g => Split(g.First().Candidates, '|'));

    private static string[] Split(string s, char sep) =>
        s.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Header candidates for a field; falls back to the shipped default when the field has no row.</summary>
    public static string[] Candidates(string kind, string field)
    {
        if (_columns.TryGetValue((kind.ToLowerInvariant(), field.ToLowerInvariant()), out var c) && c.Length > 0) return c;
        var d = DefaultColumns.FirstOrDefault(x => x.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase)
            && x.Field.Equals(field, StringComparison.OrdinalIgnoreCase));
        return d is null ? Array.Empty<string>() : Split(d.Candidates, '|');
    }

    /// <summary>True when every ';' clause has at least one '|' alternative present as an exact header.</summary>
    public static bool Matches(string signature, IReadOnlySet<string> headers)
    {
        var clauses = Split(signature, ';');
        return clauses.Length > 0 && clauses.All(cl => Split(cl, '|').Any(headers.Contains));
    }

    /// <summary>First profile (by detect order) whose signature matches the header row; null when none.</summary>
    public static ImportProfileItem? Detect(IReadOnlySet<string> headers) =>
        _profiles.FirstOrDefault(p => Matches(p.Signature, headers));

    public static IReadOnlyList<ImportProfileItem> Profiles => _profiles;
}
