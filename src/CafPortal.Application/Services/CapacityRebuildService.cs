using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Enums;
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

    // A nomination stops consuming capacity once it reaches a settled state (matches CapacityService).
    private static readonly NominationStatusType[] Settled =
    {
        NominationStatusType.Closed, NominationStatusType.Completed,
        NominationStatusType.Withdrawn, NominationStatusType.CustomerDeferred
    };

    public async Task<int> RebuildAllAsync(CancellationToken ct = default)
    {
        await _db.CapacityFacts.ExecuteDeleteAsync(ct);

        var thresholds = await _capacity.GetThresholdsAsync(ct);

        var resources = await _db.Resources.AsNoTracking()
            .Where(r => r.ActiveFlag)
            .Select(r => new { r.ResourceId, r.Role, r.CapacityLimit })
            .ToListAsync(ct);

        // Real workload = distinct in-flight accounts via nominations (Approved + not settled),
        // matching CapacityService — NOT the noisy ResourceAccounts links.
        var pairs = await _db.NominationResources.AsNoTracking()
            .Where(nr => nr.Nomination!.AccountId != null
                      && nr.Nomination.ApprovalStatus == "Approved"
                      && !Settled.Contains(nr.Nomination.Status))
            .Select(nr => new { nr.ResourceId, AccountId = nr.Nomination!.AccountId!.Value })
            .Distinct()
            .ToListAsync(ct);
        var counts = pairs.GroupBy(p => p.ResourceId).ToDictionary(g => g.Key, g => g.Count());

        var now = DateTimeOffset.UtcNow;
        // Resolve limits via the same engine CapacityService uses, so the snapshot matches the live page.
        var facts = new List<CapacityFact>(resources.Count);
        foreach (var r in resources)
        {
            var limit = await _capacity.ResolveCapacityLimitAsync(r.Role ?? string.Empty, r.CapacityLimit, ct);
            var accountCount = counts.TryGetValue(r.ResourceId, out var c) ? c : 0;
            var utilization = _capacity.CalculateUtilization(accountCount, limit);
            facts.Add(new CapacityFact
            {
                ResourceId = r.ResourceId,
                AccountCount = accountCount,
                CapacityLimit = limit,
                UtilizationPercent = utilization,
                CapacityStatus = _capacity.ResolveStatus(utilization, thresholds),
                LastUpdated = now
            });
        }

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
        var limit = await _capacity.ResolveCapacityLimitAsync(resource.Role ?? string.Empty, resource.CapacityLimit, ct);

        var accountCount = await _db.NominationResources.AsNoTracking()
            .Where(nr => nr.ResourceId == resourceId
                      && nr.Nomination!.AccountId != null
                      && nr.Nomination.ApprovalStatus == "Approved"
                      && !Settled.Contains(nr.Nomination.Status))
            .Select(nr => nr.Nomination!.AccountId!.Value)
            .Distinct().CountAsync(ct);

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
}
