using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

/// <summary>Seeds the Migration Capability masters (tools + activities). Idempotent — only fills an empty table.</summary>
public static class MigrationCapabilitySeed
{
    // (Name, Category, Vendor)
    private static readonly (string Name, string Category, string Vendor)[] Tools =
    {
        ("Azure Migrate", "Assessment", "Microsoft"),
        ("AppCAT", "Assessment", "Microsoft"),
        ("GitHub Copilot Enterprise", "GHCP", "GitHub"),
        ("GHCP Coding Agent (CI)", "GHCP", "GitHub"),
        ("GHCP Agent Mode", "GHCP", "GitHub"),
        ("AppMod .NET", "AppMod", "Microsoft"),
        ("AppMod Java", "AppMod", "Microsoft"),
        ("Upgrade Assistant", "AppMod", "Microsoft"),
        ("AppMod CLI", "AppMod", "Microsoft"),
        ("AKS Accelerator", "Accelerator", "Microsoft"),
        ("App Service Accelerator", "Accelerator", "Microsoft"),
        ("ACA Accelerator", "Accelerator", "Microsoft"),
        ("Bicep Generator", "IaC", "Microsoft"),
        ("Terraform Generator", "IaC", "HashiCorp"),
        ("Custom Prompt Framework", "Other", null!),
    };

    // (Name, Stage)
    private static readonly (string Name, string Stage)[] Activities =
    {
        ("Portfolio Assessment", "Assess"),
        ("Application Assessment", "Assess"),
        ("Dependency Analysis", "Assess"),
        ("Modernization", "Modernize"),
        ("Version Upgrade", "Modernize"),
        ("Code Remediation", "Modernize"),
        ("Containerization", "Modernize"),
        ("Target Architecture", "Architect"),
        ("IaC Generation", "Architect"),
        ("Terraform Generation", "Architect"),
        ("Bicep Generation", "Architect"),
        ("CI Pipeline", "Deploy"),
        ("CD Pipeline", "Deploy"),
        ("Deployment Automation", "Deploy"),
        ("AKS Migration", "Migrate"),
        ("ACA Migration", "Migrate"),
        ("App Service Migration", "Migrate"),
        ("ARO Migration", "Migrate"),
        ("Documentation Generation", "Operate"),
        ("Hypercare Support", "Operate"),
    };

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.MigrationTools.AnyAsync(ct))
            for (var i = 0; i < Tools.Length; i++)
                db.MigrationTools.Add(new MigrationTool
                {
                    Name = Tools[i].Name,
                    Category = Tools[i].Category,
                    Vendor = string.IsNullOrEmpty(Tools[i].Vendor) ? null : Tools[i].Vendor,
                    SortOrder = i + 1,
                });

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
}
