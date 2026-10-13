using CafPortal.Domain.Enums;

namespace CafPortal.Application.Common;

/// <summary>Snapshot of one workflow status rule.</summary>
public sealed record StatusRuleItem(NominationStatusType Status, bool IsSettled, bool IsBlocked, string Tone, bool Selectable, int SortOrder);

/// <summary>Data-driven workflow status behaviour (settled / blocked / tone / selectable), loaded from the
/// <c>StatusRules</c> table at startup and after every edit. <see cref="Defaults"/> seeds the table and covers any
/// status missing from it.</summary>
public static class StatusRules
{
    public static readonly string[] Tones = { "success", "warning", "danger", "informative", "brand", "subtle" };

    /// <summary>ApplicationSetting holding the Nominations page's default approval filter (empty = all).</summary>
    public const string DefaultApprovalKey = "DefaultApprovalFilter";
    public const string DefaultApprovalValue = "Approved";

    public static readonly IReadOnlyList<StatusRuleItem> Defaults = new StatusRuleItem[]
    {
        new(NominationStatusType.Open, false, false, "informative", true, 10),
        new(NominationStatusType.InProgress, false, false, "informative", true, 20),
        new(NominationStatusType.Blocked, false, true, "danger", true, 30),
        new(NominationStatusType.WaitingForCustomerAction, false, true, "warning", true, 40),
        new(NominationStatusType.WaitingOnFollowUp, false, true, "warning", true, 50),
        new(NominationStatusType.CustomerDeferred, true, false, "warning", true, 60),
        new(NominationStatusType.Completed, true, false, "success", true, 70),
        new(NominationStatusType.Closed, true, false, "informative", true, 80),
        new(NominationStatusType.Withdrawn, true, false, "danger", false, 90),
    };

    private static volatile IReadOnlyDictionary<NominationStatusType, StatusRuleItem> _current = Index(Defaults);
    private static volatile NominationStatusType[] _settled = SettledOf(_current);

    public static void Load(IEnumerable<StatusRuleItem> rules)
    {
        var map = Defaults.ToDictionary(d => d.Status);
        foreach (var r in rules) map[r.Status] = r;
        _current = map;
        _settled = SettledOf(map);
    }

    private static IReadOnlyDictionary<NominationStatusType, StatusRuleItem> Index(IEnumerable<StatusRuleItem> rules) =>
        rules.ToDictionary(r => r.Status);

    private static NominationStatusType[] SettledOf(IReadOnlyDictionary<NominationStatusType, StatusRuleItem> map) =>
        map.Values.Where(r => r.IsSettled).Select(r => r.Status).ToArray();

    /// <summary>Settled statuses (array form so EF can translate <c>Settled.Contains(n.Status)</c>).</summary>
    public static NominationStatusType[] Settled => _settled;

    public static IReadOnlyList<StatusRuleItem> All => _current.Values.OrderBy(r => r.SortOrder).ToList();

    public static bool IsSettled(NominationStatusType s) => _current.TryGetValue(s, out var r) && r.IsSettled;
    public static bool IsBlocked(NominationStatusType s) => _current.TryGetValue(s, out var r) && r.IsBlocked;
    public static string Tone(NominationStatusType s) => _current.TryGetValue(s, out var r) ? r.Tone : "informative";
}
