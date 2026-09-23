using CafPortal.Application.Abstractions;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>
/// Capacity engine. Utilization = ActiveAccounts / CapacityLimit. No allocation percentages.
/// All limits and thresholds are configuration-driven.
/// </summary>
public class CapacityCalculationService(IApplicationDbContext db, ISettingsProvider settings) : ICapacityCalculationService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ISettingsProvider _settings = settings;

    public async Task<CapacityThresholds> GetThresholdsAsync(CancellationToken ct = default)
    {
        var available = await _settings.GetDoubleAsync(SettingKeys.AvailableThreshold, 40, ct);
        var partiallyUtilized = await _settings.GetDoubleAsync(SettingKeys.PartiallyUtilizedThreshold, 80, ct);
        var overloaded = await _settings.GetDoubleAsync(SettingKeys.OverloadedThreshold, 100, ct);
        return new CapacityThresholds(available, partiallyUtilized, overloaded);
    }

    public async Task<int> ResolveCapacityLimitAsync(string role, int? resourceOverride, CancellationToken ct = default)
    {
        if (resourceOverride is > 0)
            return resourceOverride.Value;

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleConfig = await _db.CapacityConfigurations.AsNoTracking()
                .FirstOrDefaultAsync(c => c.RoleName == role, ct);
            if (roleConfig is { CapacityLimit: > 0 })
                return roleConfig.CapacityLimit;
        }

        return await _settings.GetIntAsync(SettingKeys.DefaultCapacityLimit, 5, ct);
    }

    public double CalculateUtilization(int accountCount, int capacityLimit)
    {
        if (capacityLimit <= 0)
            return 0;
        return Math.Round(accountCount / (double)capacityLimit * 100, 2);
    }

    public CapacityStatusType ResolveStatus(double utilizationPercent, CapacityThresholds thresholds)
    {
        if (utilizationPercent <= thresholds.AvailableMax)
            return CapacityStatusType.Available;
        if (utilizationPercent <= thresholds.PartiallyUtilizedMax)
            return CapacityStatusType.PartiallyUtilized;
        if (utilizationPercent < thresholds.OverloadedMin)
            return CapacityStatusType.FullyUtilized;
        return utilizationPercent > thresholds.OverloadedMin
            ? CapacityStatusType.Overloaded
            : CapacityStatusType.FullyUtilized;
    }

    public string ResolveHeatColor(CapacityStatusType status) => status switch
    {
        CapacityStatusType.Available => "Green",
        CapacityStatusType.PartiallyUtilized => "Amber",
        CapacityStatusType.FullyUtilized => "Amber",
        CapacityStatusType.Overloaded => "Red",
        _ => "Green"
    };
}
