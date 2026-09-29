using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class ResourceService(IApplicationDbContext db, ICapacityRebuildService capacityRebuild) : IResourceService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ICapacityRebuildService _capacityRebuild = capacityRebuild;

    public async Task<IReadOnlyList<ResourceDto>> GetAllAsync(ResourceQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resources = _db.Resources.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Region))
            resources = resources.Where(r => r.Region == query.Region);
        if (!string.IsNullOrWhiteSpace(query.Role))
            resources = resources.Where(r => r.Role == query.Role);
        if (!string.IsNullOrWhiteSpace(query.Status))
            resources = resources.Where(r => r.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Skill))
        {
            // Case-insensitive (SQLite Contains is case-sensitive), matching the name-search behavior.
            var skill = query.Skill.Trim().ToLower();
            resources = resources.Where(r =>
                (r.PrimarySkill != null && r.PrimarySkill.ToLower().Contains(skill)) ||
                (r.Skills != null && r.Skills.ToLower().Contains(skill)));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            resources = resources.Where(r =>
                r.Name.ToLower().Contains(term) ||
                (r.Psid != null && r.Psid.ToLower().Contains(term)) ||
                (r.Email != null && r.Email.ToLower().Contains(term)));
        }

        var rows = await resources
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                Resource = r,
                Capacity = _db.CapacityFacts.Where(c => c.ResourceId == r.ResourceId)
                    .OrderByDescending(c => c.Id).FirstOrDefault(),
                FallbackCount = _db.ResourceAccounts.Count(ra => ra.ResourceId == r.ResourceId),
                OnLeave = _db.LeaveFacts.Any(l => l.ResourceId == r.ResourceId && l.LeaveDate == today)
            })
            .ToListAsync(ct);

        return rows.Select(x => MapResource(x.Resource, x.Capacity, x.FallbackCount, x.OnLeave)).ToList();
    }

    public async Task<ResourceDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resource = await _db.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.ResourceId == id, ct);
        if (resource is null)
            return null;

        var capacity = await _db.CapacityFacts.AsNoTracking()
            .Where(c => c.ResourceId == id).OrderByDescending(c => c.Id).FirstOrDefaultAsync(ct);
        var fallbackCount = await _db.ResourceAccounts.CountAsync(ra => ra.ResourceId == id, ct);
        var onLeave = await _db.LeaveFacts.AnyAsync(l => l.ResourceId == id && l.LeaveDate == today, ct);

        var accounts = await _db.ResourceAccounts.AsNoTracking()
            .Where(ra => ra.ResourceId == id && ra.Account != null)
            .Select(ra => new AccountSummaryDto(
                ra.Account!.AccountId, ra.Account.AccountName, ra.Account.Region,
                ra.Account.StrategicFlag, ra.RelationshipType))
            .ToListAsync(ct);

        var upcomingLeave = await _db.LeaveFacts.AsNoTracking()
            .Where(l => l.ResourceId == id && l.LeaveDate >= today)
            .OrderBy(l => l.LeaveDate)
            .Take(30)
            .Select(l => new LeaveDto(l.Id, l.ResourceId, resource.Name, resource.Region, l.LeaveDate, l.LeaveType))
            .ToListAsync(ct);

        var baseDto = MapResource(resource, capacity, fallbackCount, onLeave);
        return new ResourceDetailDto
        {
            ResourceId = baseDto.ResourceId,
            Psid = baseDto.Psid,
            Name = baseDto.Name,
            Email = baseDto.Email,
            Mobile = baseDto.Mobile,
            Aliases = baseDto.Aliases,
            Region = baseDto.Region,
            Role = baseDto.Role,
            PrimarySkill = baseDto.PrimarySkill,
            Skills = baseDto.Skills,
            ExperienceYears = baseDto.ExperienceYears,
            Status = baseDto.Status,
            Separated = baseDto.Separated,
            CapacityLimit = baseDto.CapacityLimit,
            ActiveFlag = baseDto.ActiveFlag,
            AccountCount = baseDto.AccountCount,
            UtilizationPercent = baseDto.UtilizationPercent,
            CapacityStatus = baseDto.CapacityStatus,
            OnLeaveToday = baseDto.OnLeaveToday,
            Accounts = accounts,
            UpcomingLeave = upcomingLeave
        };
    }

    public async Task<ResourceDetailDto> CreateAsync(ResourceUpsertDto input, CancellationToken ct = default)
    {
        var resource = new Resource();
        Apply(resource, input);
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(ct);
        await _capacityRebuild.RebuildForResourceAsync(resource.ResourceId, ct);
        return (await GetByIdAsync(resource.ResourceId, ct))!;
    }

    public async Task<ResourceDetailDto?> UpdateAsync(int id, ResourceUpsertDto input, CancellationToken ct = default)
    {
        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.ResourceId == id, ct);
        if (resource is null)
            return null;
        Apply(resource, input);
        resource.UpdatedUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _capacityRebuild.RebuildForResourceAsync(id, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.ResourceId == id, ct);
        if (resource is null)
            return false;
        _db.Resources.Remove(resource); // cascades ResourceAccounts, LeaveFacts, CapacityFacts
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AssignAccountAsync(int resourceId, AssignAccountDto input, CancellationToken ct = default)
    {
        var resourceExists = await _db.Resources.AnyAsync(r => r.ResourceId == resourceId, ct);
        var accountExists = await _db.Accounts.AnyAsync(a => a.AccountId == input.AccountId, ct);
        if (!resourceExists || !accountExists)
            return false;

        var link = await _db.ResourceAccounts
            .FirstOrDefaultAsync(ra => ra.ResourceId == resourceId && ra.AccountId == input.AccountId, ct);
        if (link is null)
        {
            _db.ResourceAccounts.Add(new ResourceAccount
            {
                ResourceId = resourceId,
                AccountId = input.AccountId,
                Source = "Web",
                RelationshipType = input.RelationshipType
            });
        }
        else
        {
            link.RelationshipType = input.RelationshipType;
        }
        await _db.SaveChangesAsync(ct);
        await _capacityRebuild.RebuildForResourceAsync(resourceId, ct);
        return true;
    }

    public async Task<bool> UnassignAccountAsync(int resourceId, int accountId, CancellationToken ct = default)
    {
        var link = await _db.ResourceAccounts
            .FirstOrDefaultAsync(ra => ra.ResourceId == resourceId && ra.AccountId == accountId, ct);
        if (link is null)
            return false;
        _db.ResourceAccounts.Remove(link);
        await _db.SaveChangesAsync(ct);
        await _capacityRebuild.RebuildForResourceAsync(resourceId, ct);
        return true;
    }

    private static void Apply(Resource resource, ResourceUpsertDto input)
    {
        resource.Psid = input.Psid;
        resource.Name = input.Name;
        resource.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        resource.Mobile = string.IsNullOrWhiteSpace(input.Mobile) ? null : input.Mobile.Trim();
        resource.Aliases = string.IsNullOrWhiteSpace(input.Aliases) ? null : input.Aliases.Trim();
        resource.Region = input.Region;
        resource.Role = input.Role;
        resource.PrimarySkill = input.PrimarySkill;
        resource.Skills = input.Skills;
        resource.ExperienceYears = input.ExperienceYears;
        resource.Status = input.Status;
        resource.Separated = input.Separated;
        resource.CapacityLimit = input.CapacityLimit > 0 ? input.CapacityLimit : 5;
        resource.ActiveFlag = input.Separated ? false : input.ActiveFlag; // separated employees are always inactive
        if (!string.IsNullOrWhiteSpace(input.OnboardingStatus)
            && Enum.TryParse<CafPortal.Domain.Enums.OnboardingStatusType>(
                input.OnboardingStatus.Replace(" ", string.Empty), true, out var onboarding))
            resource.OnboardingStatus = onboarding;
    }

    private static ResourceDto MapResource(Domain.Entities.Resource r, Domain.Entities.CapacityFact? capacity,
        int fallbackCount, bool onLeave)
    {
        var accountCount = capacity?.AccountCount ?? fallbackCount;
        var limit = capacity?.CapacityLimit ?? r.CapacityLimit;
        var utilization = capacity?.UtilizationPercent
            ?? (limit > 0 ? Math.Round(accountCount / (double)limit * 100, 2) : 0);
        var status = capacity?.CapacityStatus ?? CapacityStatusType.Available;

        return new ResourceDto
        {
            ResourceId = r.ResourceId,
            Psid = r.Psid,
            Name = r.Name,
            Email = r.Email,
            Mobile = r.Mobile,
            Aliases = r.Aliases,
            Region = r.Region,
            Role = r.Role,
            PrimarySkill = r.PrimarySkill,
            Skills = r.Skills,
            ExperienceYears = r.ExperienceYears,
            Status = r.Status,
            Separated = r.Separated,
            CapacityLimit = limit,
            ActiveFlag = r.ActiveFlag,
            AccountCount = accountCount,
            UtilizationPercent = utilization,
            CapacityStatus = status.ToDisplay(),
            OnLeaveToday = onLeave,
            OnboardingStatus = r.OnboardingStatus.ToDisplay()
        };
    }
}
