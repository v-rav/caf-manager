using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class StrategicAccountService(IApplicationDbContext db) : IStrategicAccountService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<IReadOnlyList<StrategicAccountDto>> GetAsync(string? region, CancellationToken ct = default)
    {
        // Strategic accounts are those flagged either on the Account or in the configuration table.
        var configs = await _db.StrategicAccountConfigurations.AsNoTracking()
            .Where(c => c.StrategicFlag)
            .ToListAsync(ct);

        var flaggedAccounts = _db.Accounts.AsNoTracking().Where(a => a.StrategicFlag);
        if (!string.IsNullOrWhiteSpace(region))
            flaggedAccounts = flaggedAccounts.Where(a => a.Region == region);
        var accounts = await flaggedAccounts.ToListAsync(ct);

        // Merge config-only strategic accounts (by name) that may not carry the flag on the account row.
        var byName = accounts.ToDictionary(a => a.AccountName, StringComparer.OrdinalIgnoreCase);
        foreach (var cfg in configs)
        {
            if (!byName.ContainsKey(cfg.AccountName))
            {
                var match = await _db.Accounts.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AccountName == cfg.AccountName, ct);
                if (match is not null && (string.IsNullOrWhiteSpace(region) || match.Region == region))
                {
                    accounts.Add(match);
                    byName[match.AccountName] = match;
                }
            }
        }

        var result = new List<StrategicAccountDto>();
        foreach (var account in accounts.DistinctBy(a => a.AccountId))
        {
            var cfg = configs.FirstOrDefault(c =>
                c.AccountId == account.AccountId ||
                string.Equals(c.AccountName, account.AccountName, StringComparison.OrdinalIgnoreCase));

            var resourceCount = await _db.ResourceAccounts.CountAsync(ra => ra.AccountId == account.AccountId, ct);
            var recentActivity = await _db.EngagementFacts
                .Where(e => e.AccountId == account.AccountId).CountAsync(ct);
            var lastActivity = await _db.EngagementFacts
                .Where(e => e.AccountId == account.AccountId)
                .OrderByDescending(e => e.Date)
                .Select(e => (DateOnly?)e.Date)
                .FirstOrDefaultAsync(ct);

            result.Add(new StrategicAccountDto
            {
                AccountId = account.AccountId,
                AccountName = account.AccountName,
                Region = account.Region,
                PriorityWeight = cfg?.PriorityWeight ?? account.PriorityWeight,
                RiskFlag = cfg?.RiskFlag ?? false,
                ExecutiveVisibilityFlag = cfg?.ExecutiveVisibilityFlag ?? false,
                AssignedResourceCount = resourceCount,
                RecentActivityCount = recentActivity,
                LastActivityDate = lastActivity,
                RiskIndicator = ResolveRisk(resourceCount, recentActivity, cfg?.RiskFlag ?? false)
            });
        }

        return result
            .OrderByDescending(a => a.PriorityWeight)
            .ThenBy(a => a.AccountName)
            .ToList();
    }

    private static string ResolveRisk(int resourceCount, int recentActivity, bool riskFlag)
    {
        if (riskFlag || resourceCount == 0)
            return "Red";
        if (resourceCount == 1 || recentActivity == 0)
            return "Amber";
        return "Green";
    }
}
