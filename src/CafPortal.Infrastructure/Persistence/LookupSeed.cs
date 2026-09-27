using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

/// <summary>Seeds editable governance vocabularies. Idempotent per category — only fills a category that has no rows.</summary>
public static class LookupSeed
{
    public const string BlockerCategory = "BlockerCategory";
    public const string BlockerOwner = "BlockerOwner";
    public const string Classification = "Classification";
    public const string VelocityImpact = "VelocityImpact";

    private static readonly (string Category, string[] Values)[] Defaults =
    {
        (BlockerCategory, new[]
        {
            "Awaiting GHCP License", "Awaiting Customer Approval", "Awaiting Repository Access",
            "Awaiting Environment Access", "Awaiting Landing Zone", "Awaiting Security Review",
            "Awaiting Customer Testing", "Awaiting PM", "Awaiting Partner", "Internal Factory Dependency",
        }),
        (BlockerOwner, new[] { "SA", "PM", "CFTL", "Customer", "Partner", "Factory" }),
        (Classification, new[]
        {
            "Standard Factory", "Strategic Pilot", "Lighthouse Engagement", "Innovation / POC", "Recovery Engagement",
        }),
        (VelocityImpact, new[] { "Low", "Medium", "High", "Critical" }),
    };

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var (category, values) in Defaults)
        {
            if (await db.LookupValues.AnyAsync(l => l.Category == category, ct)) continue;
            for (var i = 0; i < values.Length; i++)
                db.LookupValues.Add(new LookupValue { Category = category, Value = values[i], SortOrder = i + 1 });
        }
        await db.SaveChangesAsync(ct);
    }
}
