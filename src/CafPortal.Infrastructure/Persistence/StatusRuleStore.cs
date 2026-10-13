using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

/// <summary>Seeds missing workflow status rules from <see cref="StatusRules.Defaults"/> (never overwrites edits) and
/// loads the active snapshot.</summary>
public static class StatusRuleStore
{
    public static async Task SeedAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        var present = await db.StatusRules.Select(r => r.Status).ToListAsync(ct);
        foreach (var d in StatusRules.Defaults.Where(d => !present.Contains(d.Status.ToString())))
            db.StatusRules.Add(new StatusRule
            {
                Status = d.Status.ToString(), IsSettled = d.IsSettled, IsBlocked = d.IsBlocked,
                Tone = d.Tone, Selectable = d.Selectable, SortOrder = d.SortOrder, UpdatedBy = "seed",
            });
        if (!await db.ApplicationSettings.AnyAsync(s => s.Key == StatusRules.DefaultApprovalKey, ct))
            db.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = StatusRules.DefaultApprovalKey, Value = StatusRules.DefaultApprovalValue,
                Description = "Default Approval filter on the Nominations page (empty = all)",
            });
        await db.SaveChangesAsync(ct);
    }

    public static async Task LoadAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        var rows = await db.StatusRules.AsNoTracking().ToListAsync(ct);
        StatusRules.Load(rows
            .Where(r => Enum.TryParse<NominationStatusType>(r.Status, out _))
            .Select(r => new StatusRuleItem(Enum.Parse<NominationStatusType>(r.Status), r.IsSettled, r.IsBlocked, r.Tone, r.Selectable, r.SortOrder)));
    }
}
