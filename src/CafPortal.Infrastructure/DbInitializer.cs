using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Domain.Entities.Auth;
using CafPortal.Infrastructure.Options;
using CafPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CafPortal.Infrastructure;

/// <summary>Applies migrations, seeds configuration, performs the one-time import (fresh DB), and builds the capacity snapshot.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var db = sp.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync(ct);

        var sources = sp.GetRequiredService<IOptions<SourceFileOptions>>().Value;
        var env = sp.GetRequiredService<IHostEnvironment>();
        var baseDir = Path.IsPathRooted(sources.Directory)
            ? sources.Directory
            : Path.Combine(env.ContentRootPath, sources.Directory);
        var workbookExists = File.Exists(Path.Combine(baseDir, sources.ResourceFile));
        var hasResources = await db.Resources.AnyAsync(ct);

        // Seed configuration always; seed demo data only when there is no real workbook to import.
        logger.LogInformation("Seeding configuration{Demo}...", workbookExists ? string.Empty : " and demo data");
        await SeedData.SeedAsync(db, seedDemo: !workbookExists, ct);

        // Bootstrap a default admin the first time (idempotent — only when no users exist).
        if (!await db.AppUsers.AnyAsync(ct))
        {
            db.AppUsers.Add(new AppUser
            {
                Username = "admin",
                DisplayName = "Administrator",
                PasswordHash = PasswordHashing.Hash("admin"),
                Role = UserRole.Admin,
                Active = true,
                MustChangePassword = true,
            });
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Seeded default admin (username 'admin', password 'admin') \u2014 must be changed on first login.");
        }

        // Seed the 8-gate governance template (idempotent — only when empty).
        await GateTemplateSeed.SeedAsync(db, ct);

        if (workbookExists && !hasResources)
        {
            // Fresh database + real workbook present: run the one-time import automatically.
            logger.LogInformation("Fresh database detected; performing one-time import from {File}...", sources.ResourceFile);
            var refresh = sp.GetRequiredService<IDataRefreshService>();
            var result = await refresh.RefreshAsync(ct);
            logger.LogInformation("One-time import complete. Resources={Res}, Accounts={Acc}, Links={Links}, CapacityRows={Rows}",
                result.ResourcesImported, result.AccountsImported, result.ResourceAccountLinks, result.CapacityRowsRebuilt);
        }
        else
        {
            // Existing data is the system of record — rebuild the capacity snapshot only.
            logger.LogInformation("Building capacity snapshot...");
            var rebuild = sp.GetRequiredService<ICapacityRebuildService>();
            var rows = await rebuild.RebuildAllAsync(ct);
            logger.LogInformation("Startup capacity rebuild complete. CapacityRows={Rows}", rows);
        }
    }
}
