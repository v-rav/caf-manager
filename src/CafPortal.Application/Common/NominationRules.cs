using CafPortal.Domain.Enums;

namespace CafPortal.Application.Common;

/// <summary>Single source of truth for nomination business rules shared by every service, export, import and page.
/// Computed here once and surfaced on <c>NominationDto</c> so the client never re-derives them.</summary>
public static class NominationRules
{
    /// <summary>States that stop a nomination counting as in-flight (capacity, SLA, KPIs). Data: StatusRules.</summary>
    public static NominationStatusType[] SettledStatuses => StatusRules.Settled;

    public static bool IsSettled(NominationStatusType status) => StatusRules.IsSettled(status);

    /// <summary>Classification that means "not strategic"; anything else is a strategic engagement.</summary>
    public const string StandardClassification = "Standard Factory";

    public static string ClassificationOrDefault(string? classification) =>
        string.IsNullOrWhiteSpace(classification) ? StandardClassification : classification!;

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
