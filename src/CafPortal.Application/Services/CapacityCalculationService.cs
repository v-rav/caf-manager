using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
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
        var heat = new List<string>();
        foreach (var key in new[] { "HeatAvailable", "HeatPartiallyUtilized", "HeatFullyUtilized", "HeatOverloaded" })
            heat.Add(HeatNames[Math.Clamp(await _settings.GetIntAsync(key, int.Parse(SettingsCatalog.Find(key)!.Default), ct), 0, 2)]);
        return new CapacityThresholds(available, partiallyUtilized, overloaded, heat);
    }

    private static readonly string[] HeatNames = { "Green", "Amber", "Red" };

    public async Task<int> ResolveCapacityLimitAsync(string role, int? resourceOverride, CancellationToken ct = default)
    {
        // Precedence (per CapacityRebuildService): role config > per-resource override > default.
        // The per-resource default must not short-circuit the role's configured "optimal accounts".
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleConfig = await _db.CapacityConfigurations.AsNoTracking()
                .FirstOrDefaultAsync(c => c.RoleName == role, ct);
            if (roleConfig is { CapacityLimit: > 0 })
                return roleConfig.CapacityLimit;
        }

        if (resourceOverride is > 0)
            return resourceOverride.Value;

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

    public string ResolveHeatColor(CapacityStatusType status, CapacityThresholds thresholds)
    {
        var idx = status switch
        {
            CapacityStatusType.PartiallyUtilized => 1,
            CapacityStatusType.FullyUtilized => 2,
            CapacityStatusType.Overloaded => 3,
            _ => 0,
        };
        return (thresholds.Heat is { Count: 4 } h ? h : DefaultHeat)[idx];
    }

    private static readonly string[] DefaultHeat = { "Green", "Amber", "Amber", "Red" };
}
