using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class LeaveService(IApplicationDbContext db) : ILeaveService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<LeaveWindowDto> GetWindowAsync(int windowDays, string? region, CancellationToken ct = default)
    {
        if (windowDays <= 0)
            windowDays = 30;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = today.AddDays(windowDays);

        var query = _db.LeaveFacts.AsNoTracking()
            .Where(l => l.LeaveDate >= today && l.LeaveDate <= end && l.Resource != null);
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(l => l.Resource!.Region == region);

        var items = await query
            .OrderBy(l => l.LeaveDate)
            .Select(l => new LeaveDto(l.Id, l.ResourceId, l.Resource!.Name, l.Resource.Region, l.LeaveDate, l.LeaveType))
            .ToListAsync(ct);

        return new LeaveWindowDto
        {
            WindowDays = windowDays,
            TotalLeaveDays = items.Count,
            DistinctResources = items.Select(i => i.ResourceId).Distinct().Count(),
            Items = items
        };
    }

    public async Task<LeaveDto?> CreateAsync(LeaveUpsertDto input, CancellationToken ct = default)
    {
        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourceId == input.ResourceId, ct);
        if (resource is null)
            return null;

        var leave = new LeaveFact
        {
            ResourceId = input.ResourceId,
            LeaveDate = input.LeaveDate,
            LeaveType = string.IsNullOrWhiteSpace(input.LeaveType) ? "Leave" : input.LeaveType
        };
        _db.LeaveFacts.Add(leave);
        await _db.SaveChangesAsync(ct);
        return new LeaveDto(leave.Id, resource.ResourceId, resource.Name, resource.Region, leave.LeaveDate, leave.LeaveType);
    }

    public async Task<LeaveDto?> UpdateAsync(int id, LeaveUpsertDto input, CancellationToken ct = default)
    {
        var leave = await _db.LeaveFacts.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (leave is null)
            return null;
        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourceId == input.ResourceId, ct);
        if (resource is null)
            return null;

        leave.ResourceId = input.ResourceId;
        leave.LeaveDate = input.LeaveDate;
        leave.LeaveType = string.IsNullOrWhiteSpace(input.LeaveType) ? "Leave" : input.LeaveType;
        leave.UpdatedUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new LeaveDto(leave.Id, resource.ResourceId, resource.Name, resource.Region, leave.LeaveDate, leave.LeaveType);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var leave = await _db.LeaveFacts.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (leave is null)
            return false;
        _db.LeaveFacts.Remove(leave);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
