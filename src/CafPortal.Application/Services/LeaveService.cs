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

    public async Task<IReadOnlyList<LeaveClashDto>> GetClashesAsync(string? region, CancellationToken ct = default)
    {
        var windowDays = await GetWindowSettingAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = today.AddDays(windowDays);

        var leaveQuery = _db.LeaveFacts.AsNoTracking()
            .Where(l => l.LeaveDate >= today && l.LeaveDate <= end && l.Resource != null);
        if (!string.IsNullOrWhiteSpace(region))
            leaveQuery = leaveQuery.Where(l => l.Resource!.Region == region);

        var leaves = await leaveQuery
            .Select(l => new { l.ResourceId, Name = l.Resource!.Name, l.Resource.Region, l.LeaveDate })
            .ToListAsync(ct);
        if (leaves.Count == 0)
            return Array.Empty<LeaveClashDto>();

        var ids = leaves.Select(l => l.ResourceId).Distinct().ToList();
        var activeAccounts = await _db.ResourceAccounts.AsNoTracking()
            .Where(ra => ids.Contains(ra.ResourceId))
            .GroupBy(ra => ra.ResourceId)
            .Select(g => new { ResourceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Count, ct);

        return leaves
            .GroupBy(l => new { l.ResourceId, l.Name, l.Region })
            .Where(g => activeAccounts.TryGetValue(g.Key.ResourceId, out var c) && c > 0)
            .Select(g => new LeaveClashDto(
                g.Key.ResourceId, g.Key.Name, g.Key.Region,
                activeAccounts[g.Key.ResourceId],
                g.Min(x => x.LeaveDate), g.Max(x => x.LeaveDate), g.Count()))
            .OrderByDescending(c => c.ActiveAccounts)
            .ThenBy(c => c.NextLeaveStart)
            .ToList();
    }

    private async Task<int> GetWindowSettingAsync(CancellationToken ct)
    {
        var v = await _db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key == "LeaveClashWindowDays")
            .Select(s => s.Value).FirstOrDefaultAsync(ct);
        return int.TryParse(v, out var n) && n > 0 ? n : 30;
    }

    public async Task<LeaveDto?> CreateAsync(LeaveUpsertDto input, CancellationToken ct = default)
    {
        var resource = await _db.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourceId == input.ResourceId, ct);
        if (resource is null)
            return null;

        var type = string.IsNullOrWhiteSpace(input.LeaveType) ? "Leave" : input.LeaveType;
        // A From–To range expands into one leave-day row per day (capped for safety).
        var end = input.EndDate is { } e && e > input.LeaveDate ? e : input.LeaveDate;
        if (end.DayNumber - input.LeaveDate.DayNumber > 366)
            end = input.LeaveDate.AddDays(366);

        LeaveFact? first = null;
        for (var day = input.LeaveDate; day <= end; day = day.AddDays(1))
        {
            var leave = new LeaveFact { ResourceId = input.ResourceId, LeaveDate = day, LeaveType = type };
            _db.LeaveFacts.Add(leave);
            first ??= leave;
        }
        await _db.SaveChangesAsync(ct);
        return new LeaveDto(first!.Id, resource.ResourceId, resource.Name, resource.Region, first.LeaveDate, first.LeaveType);
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
