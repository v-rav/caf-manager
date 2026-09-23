using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class CapacityService(IApplicationDbContext db, ICapacityCalculationService calc) : ICapacityService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ICapacityCalculationService _calc = calc;

    public async Task<IReadOnlyList<CapacityRowDto>> GetAsync(string? region, CancellationToken ct = default)
    {
        var resources = _db.Resources.AsNoTracking().Where(r => r.ActiveFlag);
        if (!string.IsNullOrWhiteSpace(region))
            resources = resources.Where(r => r.Region == region);

        var rows = await resources
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.ResourceId,
                r.Name,
                r.Region,
                r.Role,
                Capacity = _db.CapacityFacts.Where(c => c.ResourceId == r.ResourceId)
                    .OrderByDescending(c => c.Id).FirstOrDefault(),
                FallbackCount = _db.ResourceAccounts.Count(ra => ra.ResourceId == r.ResourceId),
                r.CapacityLimit
            })
            .ToListAsync(ct);

        return rows.Select(x =>
        {
            var accountCount = x.Capacity?.AccountCount ?? x.FallbackCount;
            var limit = x.Capacity?.CapacityLimit ?? x.CapacityLimit;
            var utilization = x.Capacity?.UtilizationPercent ?? _calc.CalculateUtilization(accountCount, limit);
            var status = x.Capacity?.CapacityStatus ?? Domain.Enums.CapacityStatusType.Available;
            return new CapacityRowDto
            {
                ResourceId = x.ResourceId,
                ResourceName = x.Name,
                Region = x.Region,
                Role = x.Role,
                AccountCount = accountCount,
                CapacityLimit = limit,
                UtilizationPercent = utilization,
                CapacityStatus = status.ToDisplay(),
                HeatColor = _calc.ResolveHeatColor(status)
            };
        }).ToList();
    }
}
