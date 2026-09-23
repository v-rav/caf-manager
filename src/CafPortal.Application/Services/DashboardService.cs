using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class DashboardService(IApplicationDbContext db) : IDashboardService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<ExecutiveDashboardDto> GetExecutiveAsync(string? region, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var resources = _db.Resources.AsNoTracking().Where(r => r.ActiveFlag);
        var accounts = _db.Accounts.AsNoTracking().AsQueryable();
        var nominations = _db.Nominations.AsNoTracking().AsQueryable();
        var leave = _db.LeaveFacts.AsNoTracking().Where(l => l.LeaveDate == today && l.Resource != null);

        if (!string.IsNullOrWhiteSpace(region))
        {
            resources = resources.Where(r => r.Region == region);
            accounts = accounts.Where(a => a.Region == region);
            nominations = nominations.Where(n => n.Region == region);
            leave = leave.Where(l => l.Resource!.Region == region);
        }

        var resourceIds = await resources.Select(r => r.ResourceId).ToListAsync(ct);

        // Latest capacity fact per resource within scope.
        var capacityByStatus = await _db.CapacityFacts.AsNoTracking()
            .Where(c => resourceIds.Contains(c.ResourceId))
            .GroupBy(c => c.ResourceId)
            .Select(g => g.OrderByDescending(c => c.Id).First().CapacityStatus)
            .GroupBy(s => s)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int CountStatus(CapacityStatusType s) => capacityByStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        var totalResources = resourceIds.Count;
        var resourcesOnLeave = await leave.Select(l => l.ResourceId).Distinct().CountAsync(ct);
        var activeAccounts = await accounts.CountAsync(ct);
        var strategicAccounts = await accounts.CountAsync(a => a.StrategicFlag, ct);
        var openNominations = await nominations.CountAsync(n => n.Status == NominationStatusType.Open, ct);

        var regionDistribution = await resources
            .GroupBy(r => r.Region)
            .Select(g => new NameValueDto(g.Key, g.Count()))
            .ToListAsync(ct);

        var available = CountStatus(CapacityStatusType.Available);
        var partial = CountStatus(CapacityStatusType.PartiallyUtilized);
        var full = CountStatus(CapacityStatusType.FullyUtilized);
        var overloaded = CountStatus(CapacityStatusType.Overloaded);

        var capacityDistribution = new List<NameValueDto>
        {
            new("Available", available),
            new("Partially Utilized", partial),
            new("Fully Utilized", full),
            new("Overloaded", overloaded)
        };

        var strategicCoverage = await accounts
            .Where(a => a.StrategicFlag)
            .OrderByDescending(a => a.PriorityWeight)
            .Take(10)
            .Select(a => new NameValueDto(
                a.AccountName,
                _db.ResourceAccounts.Count(ra => ra.AccountId == a.AccountId)))
            .ToListAsync(ct);

        return new ExecutiveDashboardDto
        {
            TotalResources = totalResources,
            ActiveAccounts = activeAccounts,
            AvailableResources = available,
            PartiallyUtilizedResources = partial,
            FullyUtilizedResources = full,
            OverloadedResources = overloaded,
            ResourcesOnLeave = resourcesOnLeave,
            StrategicAccounts = strategicAccounts,
            OpenNominations = openNominations,
            RegionDistribution = regionDistribution,
            CapacityDistribution = capacityDistribution,
            StrategicAccountCoverage = strategicCoverage
        };
    }
}
