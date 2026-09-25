using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities.Configuration;
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
                // Upsert-merge (keyed by Task Id) preserves portal edits; no destructive delete.
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
        if (result.Success)
            await StampRefreshAsync(result.CompletedUtc, ct);
        return result;
    }

    private const string LastRefreshKey = "LastDataRefreshUtc";

    public async Task<DataRefreshResultDto> UploadAndRefreshAsync(string kind, Stream content, string fileName, CancellationToken ct = default)
    {
        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only .xlsx workbooks are supported.");

        var target = kind?.Trim().ToLowerInvariant() switch
        {
            "nominations" or "nomination" => _sources.NominationFile,
            "resources" or "resource" => _sources.ResourceFile,
            "leave" => _sources.LeaveFile,
            "engagement" => _sources.EngagementFile,
            _ => throw new InvalidOperationException($"Unknown upload kind '{kind}'.")
        };

        var baseDir = Path.IsPathRooted(_sources.Directory)
            ? _sources.Directory
            : Path.Combine(_env.ContentRootPath, _sources.Directory);
        Directory.CreateDirectory(baseDir);
        var path = Path.Combine(baseDir, target);
        var tmp = Path.Combine(baseDir, target + ".upload.tmp");

        await using (var file = File.Create(tmp))
            await content.CopyToAsync(file, ct);
        File.Move(tmp, path, overwrite: true); // atomic swap; avoids partial reads mid-refresh

        _logger.LogInformation("Uploaded {Kind} workbook to {Path}; running refresh", kind, path);
        var beforeRunId = await _db.ImportRuns.MaxAsync(r => (int?)r.Id, ct) ?? 0;
        var result = await RefreshAsync(ct);
        // Stamp the uploaded file name onto the run this import just created (best-effort).
        var run = await _db.ImportRuns.OrderByDescending(r => r.Id).FirstOrDefaultAsync(ct);
        if (run is not null && run.Id > beforeRunId)
        {
            run.FileName = Path.GetFileName(fileName);
            await _db.SaveChangesAsync(ct);
        }
        return result;
    }

    public async Task<DataStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var setting = await _db.ApplicationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == LastRefreshKey, ct);
        DateTimeOffset? last = DateTimeOffset.TryParse(setting?.Value, out var dt) ? dt : null;
        return new DataStatusDto
        {
            LastRefreshUtc = last,
            Resources = await _db.Resources.CountAsync(ct),
            Accounts = await _db.Accounts.CountAsync(ct),
            Nominations = await _db.Nominations.CountAsync(ct),
            LeaveRecords = await _db.LeaveFacts.CountAsync(ct),
            PerformanceReviews = await _db.PerformanceReviews.CountAsync(ct),
        };
    }

    private async Task StampRefreshAsync(DateTimeOffset when, CancellationToken ct)
    {
        var setting = await _db.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == LastRefreshKey, ct);
        if (setting is null)
            _db.ApplicationSettings.Add(new ApplicationSetting { Key = LastRefreshKey, Value = when.ToString("O"), Description = "Timestamp of the last successful data refresh." });
        else
            setting.Value = when.ToString("O");
        await _db.SaveChangesAsync(ct);
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
