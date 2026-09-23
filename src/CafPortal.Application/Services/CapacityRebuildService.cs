using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>
/// Rebuilds CapacityFact rows. Limit precedence: role config &gt; per-resource limit &gt; default.
/// Utilization = ActiveAccounts / CapacityLimit (no allocation percentages).
/// </summary>
public class CapacityRebuildService(IApplicationDbContext db, ICapacityCalculationService capacity) : ICapacityRebuildService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ICapacityCalculationService _capacity = capacity;

    public async Task<int> RebuildAllAsync(CancellationToken ct = default)
    {
        await _db.CapacityFacts.ExecuteDeleteAsync(ct);

        var thresholds = await _capacity.GetThresholdsAsync(ct);
        var roleLimits = await _db.CapacityConfigurations.AsNoTracking()
            .ToDictionaryAsync(c => c.RoleName, c => c.CapacityLimit, StringComparer.OrdinalIgnoreCase, ct);
        var defaultLimit = await _capacity.ResolveCapacityLimitAsync(string.Empty, null, ct);

        var resources = await _db.Resources.AsNoTracking()
            .Where(r => r.ActiveFlag)
            .Select(r => new { r.ResourceId, r.Role, r.CapacityLimit })
            .ToListAsync(ct);

        var counts = await _db.ResourceAccounts.AsNoTracking()
            .GroupBy(ra => ra.ResourceId)
            .Select(g => new { ResourceId = g.Key, Count = g.Select(x => x.AccountId).Distinct().Count() })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Count, ct);

        var now = DateTimeOffset.UtcNow;
        var facts = resources.Select(r =>
        {
            var limit = ResolveLimit(roleLimits, r.Role, r.CapacityLimit, defaultLimit);
            var accountCount = counts.TryGetValue(r.ResourceId, out var c) ? c : 0;
            var utilization = _capacity.CalculateUtilization(accountCount, limit);
            return new CapacityFact
            {
                ResourceId = r.ResourceId,
                AccountCount = accountCount,
                CapacityLimit = limit,
                UtilizationPercent = utilization,
                CapacityStatus = _capacity.ResolveStatus(utilization, thresholds),
                LastUpdated = now
            };
        }).ToList();

        _db.CapacityFacts.AddRange(facts);
        await _db.SaveChangesAsync(ct);
        return facts.Count;
    }

    public async Task RebuildForResourceAsync(int resourceId, CancellationToken ct = default)
    {
        var existing = await _db.CapacityFacts.Where(c => c.ResourceId == resourceId).ToListAsync(ct);
        if (existing.Count > 0)
            _db.CapacityFacts.RemoveRange(existing);

        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourceId == resourceId, ct);
        if (resource is null || !resource.ActiveFlag)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var thresholds = await _capacity.GetThresholdsAsync(ct);
        var roleLimit = await _db.CapacityConfigurations.AsNoTracking()
            .Where(c => c.RoleName == resource.Role)
            .Select(c => (int?)c.CapacityLimit)
            .FirstOrDefaultAsync(ct);
        var defaultLimit = await _capacity.ResolveCapacityLimitAsync(string.Empty, null, ct);
        var limit = roleLimit is > 0 ? roleLimit.Value : (resource.CapacityLimit > 0 ? resource.CapacityLimit : defaultLimit);

        var accountCount = await _db.ResourceAccounts.AsNoTracking()
            .Where(ra => ra.ResourceId == resourceId)
            .Select(ra => ra.AccountId).Distinct().CountAsync(ct);

        var utilization = _capacity.CalculateUtilization(accountCount, limit);
        _db.CapacityFacts.Add(new CapacityFact
        {
            ResourceId = resourceId,
            AccountCount = accountCount,
            CapacityLimit = limit,
            UtilizationPercent = utilization,
            CapacityStatus = _capacity.ResolveStatus(utilization, thresholds),
            LastUpdated = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static int ResolveLimit(IReadOnlyDictionary<string, int> roleLimits, string? role, int resourceLimit, int defaultLimit)
        => roleLimits.TryGetValue(role ?? string.Empty, out var roleLimit) && roleLimit > 0
            ? roleLimit
            : (resourceLimit > 0 ? resourceLimit : defaultLimit);
}
