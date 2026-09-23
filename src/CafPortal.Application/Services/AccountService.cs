using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class AccountService(IApplicationDbContext db, ICapacityRebuildService capacityRebuild) : IAccountService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ICapacityRebuildService _capacityRebuild = capacityRebuild;

    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(string? search, string? region, CancellationToken ct = default)
    {
        var accounts = _db.Accounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(region))
            accounts = accounts.Where(a => a.Region == region);
        if (!string.IsNullOrWhiteSpace(search))
            accounts = accounts.Where(a => a.AccountName.Contains(search));

        return await accounts
            .OrderBy(a => a.AccountName)
            .Select(a => new AccountDto
            {
                AccountId = a.AccountId,
                AccountName = a.AccountName,
                Region = a.Region,
                Status = a.Status,
                StrategicFlag = a.StrategicFlag,
                PriorityWeight = a.PriorityWeight,
                Segment = a.Segment,
                ResourceCount = _db.ResourceAccounts.Count(ra => ra.AccountId == a.AccountId)
            })
            .ToListAsync(ct);
    }

    public async Task<AccountDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == id, ct);
        if (account is null)
            return null;

        var resources = await _db.ResourceAccounts.AsNoTracking()
            .Where(ra => ra.AccountId == id && ra.Resource != null)
            .Select(ra => new ResourceSummaryDto(
                ra.Resource!.ResourceId, ra.Resource.Name, ra.Resource.Region,
                ra.Resource.Role, ra.RelationshipType))
            .ToListAsync(ct);

        var recent = await _db.EngagementFacts.AsNoTracking()
            .Where(e => e.AccountId == id)
            .OrderByDescending(e => e.Date)
            .Take(20)
            .Select(e => new EngagementDto(
                e.Id, e.Date, e.Resource != null ? e.Resource.Name : null,
                e.MeetingName, e.Duration, e.Region, e.Remarks))
            .ToListAsync(ct);

        return new AccountDetailDto
        {
            AccountId = account.AccountId,
            AccountName = account.AccountName,
            Region = account.Region,
            Status = account.Status,
            StrategicFlag = account.StrategicFlag,
            PriorityWeight = account.PriorityWeight,
            Segment = account.Segment,
            ResourceCount = resources.Count,
            AssignedResources = resources,
            RecentActivity = recent
        };
    }

    public async Task<AccountDetailDto> CreateAsync(AccountUpsertDto input, CancellationToken ct = default)
    {
        var account = new Account();
        Apply(account, input);
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return (await GetByIdAsync(account.AccountId, ct))!;
    }

    public async Task<AccountDetailDto?> UpdateAsync(int id, AccountUpsertDto input, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == id, ct);
        if (account is null)
            return null;
        Apply(account, input);
        account.UpdatedUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == id, ct);
        if (account is null)
            return false;

        // Resources currently linked to this account need their capacity recomputed after removal.
        var affected = await _db.ResourceAccounts.AsNoTracking()
            .Where(ra => ra.AccountId == id)
            .Select(ra => ra.ResourceId).Distinct().ToListAsync(ct);

        _db.Accounts.Remove(account); // cascades ResourceAccounts
        await _db.SaveChangesAsync(ct);

        foreach (var resourceId in affected)
            await _capacityRebuild.RebuildForResourceAsync(resourceId, ct);
        return true;
    }

    private static void Apply(Account account, AccountUpsertDto input)
    {
        account.AccountName = input.AccountName;
        account.Region = input.Region;
        account.Status = input.Status;
        account.StrategicFlag = input.StrategicFlag;
        account.PriorityWeight = input.PriorityWeight > 0 ? input.PriorityWeight : 1;
        account.Segment = string.IsNullOrWhiteSpace(input.Segment) ? null : input.Segment.Trim();
    }
}
