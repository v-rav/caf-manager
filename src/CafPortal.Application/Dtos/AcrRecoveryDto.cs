namespace CafPortal.Application.Dtos;

/// <summary>One ACR recovery claim (baseline → realized), joined with its nomination for display.</summary>
public record AcrRecoveryDto(
    int Id, int NominationId, string Account, string? Tpid, string? Region, string? GapType,
    int? BaselineCores, decimal? BaselineAcr, int? RecommendedCores, decimal? RecommendedAcr,
    DateTime FlaggedUtc, string? FlaggedBy, DateTime? NotifiedUtc, string? NotifiedBy,
    int? RealizedCores, decimal? RealizedAcr, DateTime? RealizedUtc, decimal? RecoveredAcr,
    decimal? CurrentAcr, string Status);

/// <summary>Recovery funnel + the headline "ACR claimed because of the analysis".</summary>
public record AcrRecoverySummaryDto(
    int FlaggedCount, int NotifiedCount, int RealizedCount, int ClosedCount,
    decimal BaselineAcrOpen, decimal RecommendedAcrOpen, decimal RecoveredAcrTotal,
    double RecoveryRatePct, double? AvgDaysToRealize, double? EstimateAccuracyPct);

/// <summary>Creates a recovery claim; baseline is snapshotted server-side from the nomination.</summary>
public record AcrRecoveryFlagRequest(int NominationId, int? RecommendedCores, decimal? RecommendedAcr, string? GapType);
