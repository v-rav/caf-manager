using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

/// <summary>Seeds the Migration Capability masters (tools + activities). While no usage exists the masters are
/// reconciled to the canonical lists (taxonomy still malleable); once usage is captured seeding is additive-only.</summary>
public static class MigrationCapabilitySeed
{
    // (Name, Category, Vendor). AppMod is a GHCP-powered capability, so it lives under GHCP (not its own category).
    private static readonly (string Name, string Category, string? Vendor)[] Tools =
    {
        ("Azure Migrate", "Assessment", "Microsoft"),
        ("AppCAT", "Assessment", "Microsoft"),
        ("GHCP-CI", "GHCP", "GitHub"),
        ("GHCP Agent Mode", "GHCP", "GitHub"),
        ("GHCP Custom Prompts", "GHCP", "GitHub"),
        ("AppMod .NET", "GHCP", "Microsoft"),
        ("AppMod Java", "GHCP", "Microsoft"),
        ("AppMod CLI", "GHCP", "Microsoft"),
        ("Upgrade Assistant", "GHCP", "Microsoft"),
        ("AKS Accelerator", "Accelerator", "Microsoft"),
        ("ACA Accelerator", "Accelerator", "Microsoft"),
        ("App Service Accelerator", "Accelerator", "Microsoft"),
        ("Partner Tooling", "Other", null),
        ("Customer Tooling", "Other", null),
        ("Internal Automation", "Other", null),
    };

    // (Name, Stage). Activities are independent of tools — no capability matrix.
    private static readonly (string Name, string Stage)[] Activities =
    {
        ("Portfolio Assessment", "Assessment"),
        ("Application Assessment", "Assessment"),
        ("Dependency Analysis", "Assessment"),
        ("Migration Strategy", "Planning & Architecture"),
        ("Target Architecture", "Planning & Architecture"),
        ("Version Upgrade", "Modernization"),
        ("Modernization", "Modernization"),
        ("Code Remediation", "Modernization"),
        ("Containerization", "Modernization"),
        ("IaC Generation", "Engineering Automation"),
        ("CI Pipeline", "Engineering Automation"),
        ("CD Pipeline", "Engineering Automation"),
        ("Deployment Automation", "Engineering Automation"),
        ("AKS Migration", "Migration"),
        ("ACA Migration", "Migration"),
        ("App Service Migration", "Migration"),
        ("ARO Migration", "Migration"),
        ("Testing & Validation", "Operations"),
        ("Documentation Generation", "Operations"),
        ("Hypercare Support", "Operations"),
    };

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Only reconcile (replace) the masters while nothing references them; afterwards, seed empty tables only.
        var hasUsage = await db.NominationToolUsages.AnyAsync(ct);

        var toolNames = await db.MigrationTools.Select(t => t.Name).ToListAsync(ct);
        if (!hasUsage && !SameSet(toolNames, Tools.Select(t => t.Name)))
        {
            db.MigrationTools.RemoveRange(await db.MigrationTools.ToListAsync(ct));
            await db.SaveChangesAsync(ct);
        }
        if (!await db.MigrationTools.AnyAsync(ct))
            for (var i = 0; i < Tools.Length; i++)
                db.MigrationTools.Add(new MigrationTool
                {
                    Name = Tools[i].Name,
                    Category = Tools[i].Category,
                    Vendor = string.IsNullOrEmpty(Tools[i].Vendor) ? null : Tools[i].Vendor,
                    SortOrder = i + 1,
                });

        var actNames = await db.MigrationActivities.Select(a => a.Name).ToListAsync(ct);
        if (!hasUsage && !SameSet(actNames, Activities.Select(a => a.Name)))
        {
            db.MigrationActivities.RemoveRange(await db.MigrationActivities.ToListAsync(ct));
            await db.SaveChangesAsync(ct);
        }
        if (!await db.MigrationActivities.AnyAsync(ct))
            for (var i = 0; i < Activities.Length; i++)
                db.MigrationActivities.Add(new MigrationActivity
                {
                    Name = Activities[i].Name,
                    Stage = Activities[i].Stage,
                    SortOrder = i + 1,
                });

        await db.SaveChangesAsync(ct);
    }

    private static bool SameSet(IEnumerable<string> a, IEnumerable<string> b)
        => new HashSet<string>(a).SetEquals(b);
}
