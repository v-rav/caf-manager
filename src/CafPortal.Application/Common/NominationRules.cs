using CafPortal.Domain.Enums;

namespace CafPortal.Application.Common;

/// <summary>Single source of truth for nomination business rules shared by every service, export, import and page.
/// Computed here once and surfaced on <c>NominationDto</c> so the client never re-derives them.</summary>
public static class NominationRules
{
    /// <summary>States that stop a nomination counting as in-flight (capacity, SLA, KPIs).</summary>
    public static readonly NominationStatusType[] SettledStatuses =
    {
        NominationStatusType.Closed, NominationStatusType.Completed,
        NominationStatusType.Withdrawn, NominationStatusType.CustomerDeferred,
    };

    public static bool IsSettled(NominationStatusType status) => SettledStatuses.Contains(status);

    /// <summary>Classification that means "not strategic"; anything else is a strategic engagement.</summary>
    public const string StandardClassification = "Standard Factory";

    public static string ClassificationOrDefault(string? classification) =>
        string.IsNullOrWhiteSpace(classification) ? StandardClassification : classification!;

    public static bool IsStrategic(string? classification) =>
        !ClassificationOrDefault(classification).Equals(StandardClassification, StringComparison.OrdinalIgnoreCase);

    public static readonly string[] StageLabels =
        { "Validating", "Executing Pre-Requisites", "Finalize Scope", "Executing Migration" };

    /// <summary>FDO Migration Status text → stage 1–4 (null when unrecognised).</summary>
    public static int? StageIndex(string? migration)
    {
        var m = migration?.ToLowerInvariant() ?? string.Empty;
        if (m.Contains("validating")) return 1;
        if (m.Contains("pre-requisite") || m.Contains("pre requisite") || m.Contains("prerequisite")) return 2;
        if (m.Contains("finalize")) return 3;
        if (m.Contains("executing migration")) return 4;
        return null;
    }

    public static string StageLabel(int? stage) =>
        stage is >= 1 and <= 4 ? StageLabels[stage.Value - 1] : string.Empty;

    /// <summary>FDO Current State text → health bucket: Blocked · Waiting · On Track · Other.</summary>
    public static string HealthBucket(string? currentState)
    {
        var t = (currentState ?? string.Empty).ToLowerInvariant();
        if (t.Contains("block")) return "Blocked";
        if (t.Contains("waiting") || t.Contains("follow")) return "Waiting";
        if (t.Contains("on track")) return "On Track";
        return "Other";
    }
}
