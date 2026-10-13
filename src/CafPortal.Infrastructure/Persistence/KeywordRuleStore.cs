using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

/// <summary>Seeds the KeywordRules table from <see cref="KeywordRules.Defaults"/> (per empty domain, so admin edits are
/// never overwritten) and loads the active rules into the in-memory evaluation snapshot.</summary>
public static class KeywordRuleStore
{
    public static async Task SeedAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        var seeded = await db.KeywordRules.Select(r => r.Domain).Distinct().ToListAsync(ct);
        foreach (var rule in KeywordRules.Defaults.Where(d => !seeded.Contains(d.Domain)))
            db.KeywordRules.Add(new KeywordRule
            {
                Domain = rule.Domain, Pattern = rule.Pattern, MatchType = rule.MatchType,
                Target = rule.Target, Priority = rule.Priority, UpdatedBy = "seed",
            });
        await db.SaveChangesAsync(ct);
    }

    public static async Task LoadAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        var rules = await db.KeywordRules.AsNoTracking()
            .Where(r => r.ActiveFlag)
            .Select(r => new KeywordRuleItem(r.Domain, r.Pattern, r.MatchType, r.Target, r.Priority))
            .ToListAsync(ct);
        KeywordRules.Load(rules);
    }
}
