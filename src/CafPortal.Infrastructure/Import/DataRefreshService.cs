using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Infrastructure.Options;
using CafPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CafPortal.Infrastructure.Import;

/// <summary>
/// One-time / on-demand import pipeline: load each source workbook (when present),
/// transform, then rebuild the materialized CapacityFact snapshot. Triggered manually
/// (Refresh button / POST /api/admin/refresh); the web app is the system of record afterwards.
/// </summary>
public class DataRefreshService(
    AppDbContext db,
    IResourceImportService resourceImport,
    ILeaveImportService leaveImport,
    IEngagementImportService engagementImport,
    INominationImportService nominationImport,
    ICapacityRebuildService capacityRebuild,
    IHostEnvironment env,
    IOptions<SourceFileOptions> sourceOptions,
    ILogger<DataRefreshService> logger) : IDataRefreshService
{
    private readonly AppDbContext _db = db;
    private readonly IResourceImportService _resourceImport = resourceImport;
    private readonly ILeaveImportService _leaveImport = leaveImport;
    private readonly IEngagementImportService _engagementImport = engagementImport;
    private readonly INominationImportService _nominationImport = nominationImport;
    private readonly ICapacityRebuildService _capacityRebuild = capacityRebuild;
    private readonly IHostEnvironment _env = env;
    private readonly SourceFileOptions _sources = sourceOptions.Value;
    private readonly ILogger<DataRefreshService> _logger = logger;

    public async Task<DataRefreshResultDto> RefreshAsync(CancellationToken ct = default)
    {
        var result = new DataRefreshResultDto { StartedUtc = DateTimeOffset.UtcNow };

        try
        {
            var baseDir = Path.IsPathRooted(_sources.Directory)
                ? _sources.Directory
                : Path.Combine(_env.ContentRootPath, _sources.Directory);

            var resourcePath = Path.Combine(baseDir, _sources.ResourceFile);
            var leavePath = Path.Combine(baseDir, _sources.LeaveFile);
            var engagementPath = Path.Combine(baseDir, _sources.EngagementFile);
            var nominationPath = Path.Combine(baseDir, _sources.NominationFile);

            // Only clear a fact table when its source file exists; otherwise preserve seeded/manual data.
            if (File.Exists(resourcePath))
            {
                await _db.ResourceAccounts.ExecuteDeleteAsync(ct);
                result.ResourcesImported = await TryImportAsync(resourcePath, _resourceImport, "Resource mapping", result, ct);
            }
            else
            {
                result.Messages.Add($"Resource mapping: source file not found ({_sources.ResourceFile}), preserved existing data.");
            }

            if (File.Exists(leavePath))
            {
                await _db.LeaveFacts.ExecuteDeleteAsync(ct);
                result.LeaveRecords = await TryImportAsync(leavePath, _leaveImport, "Leave calendar", result, ct);
            }
            else
            {
                result.Messages.Add($"Leave calendar: source file not found ({_sources.LeaveFile}), preserved existing data.");
            }

            if (File.Exists(engagementPath))
            {
                await _db.EngagementFacts.ExecuteDeleteAsync(ct);
                result.EngagementRecords = await TryImportAsync(engagementPath, _engagementImport, "Time-hunt tracking", result, ct);
            }
            else
            {
                result.Messages.Add($"Time-hunt tracking: source file not found ({_sources.EngagementFile}), preserved existing data.");
            }

            if (File.Exists(nominationPath))
            {
                await _db.Nominations.ExecuteDeleteAsync(ct);
                result.NominationRecords = await TryImportAsync(nominationPath, _nominationImport, "Nomination pipeline", result, ct);
            }
            else
            {
                result.Messages.Add($"Nomination pipeline: source file not found ({_sources.NominationFile}), preserved existing data.");
            }

            result.AccountsImported = await _db.Accounts.CountAsync(ct);
            result.ResourceAccountLinks = await _db.ResourceAccounts.CountAsync(ct);
            await SyncStrategicFlagsAsync(ct);
            result.CapacityRowsRebuilt = await _capacityRebuild.RebuildAllAsync(ct);

            result.Messages.Add("Capacity facts rebuilt; cache cleared.");
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data refresh failed");
            result.Success = false;
            result.Messages.Add($"Refresh failed: {ex.Message}");
        }

        result.CompletedUtc = DateTimeOffset.UtcNow;
        return result;
    }

    private async Task<int> TryImportAsync(string path, dynamic service, string label,
        DataRefreshResultDto result, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            result.Messages.Add($"{label}: source file not found ({Path.GetFileName(path)}), skipped.");
            return 0;
        }

        await using var stream = File.OpenRead(path);
        int count = await service.ImportAsync(stream, ct);
        result.Messages.Add($"{label}: imported {count} rows.");
        return count;
    }

    /// <summary>Flags imported accounts as strategic when their canonical name matches the strategic config list.</summary>
    private async Task SyncStrategicFlagsAsync(CancellationToken ct)
    {
        var strategicNames = await _db.StrategicAccountConfigurations.AsNoTracking()
            .Where(c => c.StrategicFlag)
            .Select(c => c.AccountName)
            .ToListAsync(ct);
        if (strategicNames.Count == 0)
            return;

        var set = new HashSet<string>(strategicNames, StringComparer.OrdinalIgnoreCase);
        var accounts = await _db.Accounts.Where(a => !a.StrategicFlag).ToListAsync(ct);
        foreach (var account in accounts.Where(a => set.Contains(a.AccountName)))
        {
            account.StrategicFlag = true;
            if (account.PriorityWeight < 2)
                account.PriorityWeight = 2;
        }
        await _db.SaveChangesAsync(ct);
    }
}
