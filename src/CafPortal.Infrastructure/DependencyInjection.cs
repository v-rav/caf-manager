using CafPortal.Application.Abstractions;
using CafPortal.Infrastructure.Import;
using CafPortal.Infrastructure.Jobs;
using CafPortal.Infrastructure.Options;
using CafPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CafPortal.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers EF Core (SQLite), import + refresh services, and the daily background job.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=App_Data/cafdb.sqlite";

        EnsureSqliteDirectoryExists(connectionString);

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<SourceFileOptions>(configuration.GetSection(SourceFileOptions.SectionName));

        services.AddScoped<IResourceImportService, ResourceImportService>();
        services.AddScoped<ILeaveImportService, LeaveImportService>();
        services.AddScoped<IEngagementImportService, EngagementImportService>();
        services.AddScoped<INominationImportService, NominationImportService>();
        services.AddScoped<IAccountMasterImportService, AccountMasterImportService>();
        services.AddScoped<IDataRefreshService, DataRefreshService>();
        services.AddScoped<IExportService, Export.ExportService>();
        services.AddScoped<IBackupService, Backup.BackupService>();

        // The web app is the system of record; the nightly import job is opt-in.
        if (configuration.GetValue("BackgroundRefresh:Enabled", false))
            services.AddHostedService<DailyRefreshHostedService>();

        return services;
    }

    /// <summary>SQLite creates the file but not its parent folder; make sure it exists.</summary>
    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
            return;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}
