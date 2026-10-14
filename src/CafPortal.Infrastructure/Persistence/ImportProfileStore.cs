using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

/// <summary>Seeds missing import profiles / column maps from <see cref="ImportProfiles"/> defaults (admin edits are never
/// overwritten) and loads them into the in-memory snapshot used by file detection and the importers.</summary>
public static class ImportProfileStore
{
    public static async Task SeedAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        var kinds = await db.ImportProfiles.Select(p => p.Kind).ToListAsync(ct);
        foreach (var p in ImportProfiles.DefaultProfiles.Where(p => !kinds.Contains(p.Kind)))
            db.ImportProfiles.Add(new ImportProfile { Kind = p.Kind, Label = p.Label, DetectOrder = p.DetectOrder, Signature = p.Signature, UpdatedBy = "seed" });

        var fields = (await db.ImportColumnMaps.Select(c => new { c.Kind, c.Field }).ToListAsync(ct))
            .Select(c => (c.Kind.ToLowerInvariant(), c.Field.ToLowerInvariant())).ToHashSet();
        foreach (var c in ImportProfiles.DefaultColumns.Where(c => !fields.Contains((c.Kind.ToLowerInvariant(), c.Field.ToLowerInvariant()))))
            db.ImportColumnMaps.Add(new ImportColumnMap { Kind = c.Kind, Field = c.Field, Candidates = c.Candidates, SortOrder = c.SortOrder, UpdatedBy = "seed" });

        await db.SaveChangesAsync(ct);
    }

    public static async Task LoadAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        var profiles = await db.ImportProfiles.AsNoTracking()
            .Select(p => new ImportProfileItem(p.Kind, p.Label, p.DetectOrder, p.Signature)).ToListAsync(ct);
        var columns = await db.ImportColumnMaps.AsNoTracking()
            .Select(c => new ImportColumnItem(c.Kind, c.Field, c.Candidates, c.SortOrder)).ToListAsync(ct);
        ImportProfiles.Load(profiles, columns);
    }
}
