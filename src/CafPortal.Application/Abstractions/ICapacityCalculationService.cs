using CafPortal.Domain.Enums;

namespace CafPortal.Application.Abstractions;

/// <summary>Configuration-driven thresholds resolved once and reused for a calculation batch. <c>Heat</c> = heat colour per
/// status in enum order (Available, Partially, Fully, Overloaded).</summary>
public sealed record CapacityThresholds(double AvailableMax, double PartiallyUtilizedMax, double OverloadedMin,
    IReadOnlyList<string>? Heat = null);

/// <summary>Pure capacity math. Thresholds and limits are supplied, never hardcoded here.</summary>
public interface ICapacityCalculationService
{
    Task<CapacityThresholds> GetThresholdsAsync(CancellationToken ct = default);
    Task<int> ResolveCapacityLimitAsync(string role, int? resourceOverride, CancellationToken ct = default);

    double CalculateUtilization(int accountCount, int capacityLimit);
    CapacityStatusType ResolveStatus(double utilizationPercent, CapacityThresholds thresholds);
    string ResolveHeatColor(CapacityStatusType status, CapacityThresholds thresholds);
}
