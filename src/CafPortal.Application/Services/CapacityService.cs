using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class CapacityService(IApplicationDbContext db, ICapacityCalculationService calc) : ICapacityService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ICapacityCalculationService _calc = calc;

    // A nomination stops consuming capacity once it reaches a settled state.
    private static readonly NominationStatusType[] Settled =
    {
        NominationStatusType.Closed, NominationStatusType.Completed,
        NominationStatusType.Withdrawn, NominationStatusType.CustomerDeferred
    };

    public async Task<IReadOnlyList<CapacityRowDto>> GetAsync(string? region, CancellationToken ct = default)
    {
        var thresholds = await _calc.GetThresholdsAsync(ct);

        // Real workload = distinct in-flight accounts a resource is actually assigned to
        // (NominationResource -> Nomination -> Account), not the noisy ResourceAccounts links.
        var pairs = await _db.NominationResources
            .Where(nr => nr.Nomination!.AccountId != null
                      && nr.Nomination.ApprovalStatus == "Approved"
                      && !Settled.Contains(nr.Nomination.Status))
            .Select(nr => new { nr.ResourceId, AccountId = nr.Nomination!.AccountId!.Value })
            .Distinct()
            .ToListAsync(ct);
        var countByResource = pairs.GroupBy(p => p.ResourceId).ToDictionary(g => g.Key, g => g.Count());

        // Account names for the Accounts hover (which accounts each resource is actually assigned to).
        var accountIds = pairs.Select(p => p.AccountId).Distinct().ToList();
        var accountNames = await _db.Accounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.AccountId))
            .Select(a => new { a.AccountId, a.AccountName })
            .ToDictionaryAsync(a => a.AccountId, a => a.AccountName, ct);
        var accountsByResource = pairs
            .GroupBy(p => p.ResourceId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .Select(p => accountNames.TryGetValue(p.AccountId, out var n) ? n : $"#{p.AccountId}")
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList());

        var resources = _db.Resources.AsNoTracking().Where(r => r.ActiveFlag);
        if (!string.IsNullOrWhiteSpace(region))
            resources = resources.Where(r => r.Region == region);

        var list = await resources
            .OrderBy(r => r.Name)
            .Select(r => new { r.ResourceId, r.Name, r.Region, r.Role, r.CapacityLimit })
            .ToListAsync(ct);

        var rows = new List<CapacityRowDto>(list.Count);
        foreach (var r in list)
        {
            var accountCount = countByResource.TryGetValue(r.ResourceId, out var c) ? c : 0;
            var limit = await _calc.ResolveCapacityLimitAsync(r.Role ?? string.Empty, r.CapacityLimit, ct);
            var utilization = _calc.CalculateUtilization(accountCount, limit);
            var status = _calc.ResolveStatus(utilization, thresholds);
            rows.Add(new CapacityRowDto
            {
                ResourceId = r.ResourceId,
                ResourceName = r.Name,
                Region = r.Region ?? string.Empty,
                Role = r.Role,
                AccountCount = accountCount,
                Accounts = accountsByResource.TryGetValue(r.ResourceId, out var accs) ? accs : [],
                CapacityLimit = limit,
                UtilizationPercent = utilization,
                CapacityStatus = status.ToDisplay(),
                HeatColor = _calc.ResolveHeatColor(status)
            });
        }
        return rows;
    }
}
