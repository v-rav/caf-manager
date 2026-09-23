using CafPortal.Domain.Enums;

namespace CafPortal.Application.Abstractions;

/// <summary>Configuration-driven thresholds resolved once and reused for a calculation batch.</summary>
public sealed record CapacityThresholds(double AvailableMax, double PartiallyUtilizedMax, double OverloadedMin);

/// <summary>Pure capacity math. Thresholds and limits are supplied, never hardcoded here.</summary>
public interface ICapacityCalculationService
{
    Task<CapacityThresholds> GetThresholdsAsync(CancellationToken ct = default);
    Task<int> ResolveCapacityLimitAsync(string role, int? resourceOverride, CancellationToken ct = default);

    double CalculateUtilization(int accountCount, int capacityLimit);
    CapacityStatusType ResolveStatus(double utilizationPercent, CapacityThresholds thresholds);
    string ResolveHeatColor(CapacityStatusType status);
}
