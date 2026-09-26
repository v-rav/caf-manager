using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Domain.Enums;
using CafPortal.Infrastructure.Options;
using CafPortal.Infrastructure.Persistence;
using ClosedXML.Excel;
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
    IAccountMasterImportService accountMasterImport,
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
    private readonly IAccountMasterImportService _accountMasterImport = accountMasterImport;
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

            var leavePath = Path.Combine(baseDir, _sources.LeaveFile);
            var engagementPath = Path.Combine(baseDir, _sources.EngagementFile);
            var nominationPath = Path.Combine(baseDir, _sources.NominationFile);
            var accountMasterPath = Path.Combine(baseDir, _sources.AccountMasterFile);

            // Resources are the system of record in the app: managed manually or via an explicit
            // Excel upload only. The general refresh never imports/overwrites the resources table.
            result.Messages.Add("Resources: not refreshed (manual / Excel-upload only).");

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

            // Account master list enriches accounts (canonical name + segment); additive upsert, runs after nominations.
            if (File.Exists(accountMasterPath))
            {
                await TryImportAsync(accountMasterPath, _accountMasterImport, "Account master", result, ct);
            }
            else
            {
                result.Messages.Add($"Account master: source file not found ({_sources.AccountMasterFile}), preserved existing data.");
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

        var normalizedKind = kind?.Trim().ToLowerInvariant();

        // Resources are manual / upload-only: import the uploaded workbook directly (one-shot) and
        // never stage it into SourceData, where the general refresh would otherwise re-import it.
        if (normalizedKind is "resources" or "resource")
            return await ImportResourcesUploadAsync(content, fileName, ct);

        var target = normalizedKind switch
        {
            "nominations" or "nomination" => _sources.NominationFile,
            "leave" => _sources.LeaveFile,
            "engagement" => _sources.EngagementFile,
            "accounts" or "account-master" or "accountmaster" => _sources.AccountMasterFile,
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

    // Imports an uploaded resources workbook directly (upsert), then rebuilds capacity. Not staged into
    // SourceData, so it never becomes a recurring auto-source — the resources table stays manual/upload-only.
    private async Task<DataRefreshResultDto> ImportResourcesUploadAsync(Stream content, string fileName, CancellationToken ct)
    {
        var result = new DataRefreshResultDto { StartedUtc = DateTimeOffset.UtcNow };
        try
        {
            using var buffer = new MemoryStream(); // buffer so the workbook reader can seek the upload stream
            await content.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            result.ResourcesImported = await _resourceImport.ImportAsync(buffer, ct);
            result.ResourceAccountLinks = await _db.ResourceAccounts.CountAsync(ct);
            result.AccountsImported = await _db.Accounts.CountAsync(ct);
            await SyncStrategicFlagsAsync(ct);
            result.CapacityRowsRebuilt = await _capacityRebuild.RebuildAllAsync(ct);
            result.Messages.Add($"Resources imported from upload ({Path.GetFileName(fileName)}); capacity rebuilt.");
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resource upload import failed");
            result.Success = false;
            result.Messages.Add($"Resource import failed: {ex.Message}");
        }
        result.CompletedUtc = DateTimeOffset.UtcNow;
        if (result.Success)
            await StampRefreshAsync(result.CompletedUtc, ct);
        return result;
    }

    // Re-runs the one-time seed on demand (config + resource enrichments), then rebuilds capacity.
    public async Task<DataRefreshResultDto> ReseedAsync(CancellationToken ct = default)
    {
        var result = new DataRefreshResultDto { StartedUtc = DateTimeOffset.UtcNow };
        try
        {
            await Persistence.SeedData.SeedAsync(_db, seedDemo: false, force: true, ct);
            await SyncStrategicFlagsAsync(ct);
            result.CapacityRowsRebuilt = await _capacityRebuild.RebuildAllAsync(ct);
            result.Messages.Add("Re-seed complete (configuration + resource enrichments); capacity rebuilt.");
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Re-seed failed");
            result.Success = false;
            result.Messages.Add($"Re-seed failed: {ex.Message}");
        }
        result.CompletedUtc = DateTimeOffset.UtcNow;
        return result;
    }

    // Merges casing/punctuation-variant duplicate accounts into the TPID-bearing master.
    // Skips non-Latin names (normalize to empty) and any group with >=2 distinct TPIDs. apply=false previews.
    public async Task<DataRefreshResultDto> MergeDuplicateAccountsAsync(bool apply, CancellationToken ct = default)
    {
        static string Norm(string? s) => new string((s ?? string.Empty).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        var result = new DataRefreshResultDto { StartedUtc = DateTimeOffset.UtcNow };
        try
        {
            var accounts = await _db.Accounts.ToListAsync(ct);
            var groups = accounts
                .Where(a => Norm(a.AccountName).Length > 0) // skip non-Latin names that normalize to empty (avoids CJK false positives)
                .GroupBy(a => Norm(a.AccountName))
                .Where(g => g.Count() > 1)
                .ToList();

            int merged = 0, skipped = 0, noms = 0, links = 0, waves = 0, engs = 0, owns = 0, strat = 0;
            foreach (var g in groups)
            {
                var tpidHolders = g.Where(a => !string.IsNullOrWhiteSpace(a.Tpid)).ToList();
                var distinctTpids = tpidHolders.Select(a => a.Tpid!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if (distinctTpids >= 2)
                {
                    skipped++;
                    result.Messages.Add($"Skipped '{g.First().AccountName}' — {distinctTpids} distinct TPIDs (treated as separate accounts).");
                    continue;
                }
                var survivor = tpidHolders.Count == 1
                    ? tpidHolders[0]
                    : g.OrderBy(a => a.AccountId).First();
                var casualties = g.Where(a => a.AccountId != survivor.AccountId).ToList();

                var survivorResourceIds = (await _db.ResourceAccounts.Where(ra => ra.AccountId == survivor.AccountId)
                    .Select(ra => ra.ResourceId).ToListAsync(ct)).ToHashSet();

                foreach (var cas in casualties)
                {
                    foreach (var n in await _db.Nominations.Where(x => x.AccountId == cas.AccountId).ToListAsync(ct))
                    { if (apply) n.AccountId = survivor.AccountId; noms++; }
                    foreach (var w in await _db.WaveLinks.Where(x => x.AccountId == cas.AccountId).ToListAsync(ct))
                    { if (apply) w.AccountId = survivor.AccountId; waves++; }
                    foreach (var e in await _db.EngagementFacts.Where(x => x.AccountId == cas.AccountId).ToListAsync(ct))
                    { if (apply) e.AccountId = survivor.AccountId; engs++; }
                    foreach (var o in await _db.OwnershipHistory.Where(x => x.AccountId == cas.AccountId).ToListAsync(ct))
                    { if (apply) o.AccountId = survivor.AccountId; owns++; }
                    foreach (var s in await _db.StrategicAccountConfigurations.Where(x => x.AccountId == cas.AccountId).ToListAsync(ct))
                    { if (apply) s.AccountId = survivor.AccountId; strat++; }

                    foreach (var link in await _db.ResourceAccounts.Where(x => x.AccountId == cas.AccountId).ToListAsync(ct))
                    {
                        if (survivorResourceIds.Contains(link.ResourceId))
                        { if (apply) _db.ResourceAccounts.Remove(link); } // dedup: survivor already linked to this resource
                        else
                        { if (apply) { link.AccountId = survivor.AccountId; survivorResourceIds.Add(link.ResourceId); } links++; }
                    }

                    if (apply)
                    {
                        var aliases = (survivor.Aliases ?? string.Empty)
                            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                        void AddAlias(string? a)
                        {
                            if (!string.IsNullOrWhiteSpace(a) && !aliases.Any(x => string.Equals(x, a, StringComparison.OrdinalIgnoreCase)))
                                aliases.Add(a.Trim());
                        }
                        AddAlias(cas.AccountName);
                        foreach (var ca in (cas.Aliases ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            AddAlias(ca);
                        survivor.Aliases = aliases.Count > 0 ? string.Join("; ", aliases) : null;

                        if (string.IsNullOrWhiteSpace(survivor.Tpid) && !string.IsNullOrWhiteSpace(cas.Tpid)) survivor.Tpid = cas.Tpid;
                        if (string.IsNullOrWhiteSpace(survivor.Segment) && !string.IsNullOrWhiteSpace(cas.Segment)) survivor.Segment = cas.Segment;
                        if ((string.IsNullOrWhiteSpace(survivor.Region) || survivor.Region == "UNSPECIFIED") && !string.IsNullOrWhiteSpace(cas.Region))
                            survivor.Region = cas.Region;

                        _db.Accounts.Remove(cas);
                    }
                    merged++;
                    result.Messages.Add($"{(apply ? "Merged" : "Would merge")} '{cas.AccountName}' -> '{survivor.AccountName}' (tpid {survivor.Tpid ?? cas.Tpid}).");
                }
            }

            if (apply && merged > 0)
            {
                await _db.SaveChangesAsync(ct);
                await SyncStrategicFlagsAsync(ct);
                await _capacityRebuild.RebuildAllAsync(ct);
            }
            result.Success = true;
            result.Messages.Add($"{(apply ? "Merged" : "Preview")}: {merged} account(s), {skipped} group(s) skipped. Re-pointed noms={noms}, links={links}, waves={waves}, engagements={engs}, ownership={owns}, strategic={strat}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account merge failed");
            result.Success = false;
            result.Messages.Add($"Account merge failed: {ex.Message}");
        }
        result.CompletedUtc = DateTimeOffset.UtcNow;
        return result;
    }

    // Moves no-TPID accounts (non-canonical: departments / apps / abbreviations) out of the master into
    // ParkedAccount, detaching references (nullable FKs nulled; non-null ResourceAccount/OwnershipHistory
    // deleted after being recorded for restore). Fully reversible via UnparkAllAccountsAsync. apply=false previews.
    public async Task<DataRefreshResultDto> ParkNoTpidAccountsAsync(bool apply, CancellationToken ct = default)
    {
        var result = new DataRefreshResultDto { StartedUtc = DateTimeOffset.UtcNow };
        try
        {
            // Keep any no-TPID account a nomination still points at (parking it would orphan the nomination);
            // the next FDO upload is expected to resolve those to a real TPID.
            var referenced = (await _db.Nominations.Where(n => n.AccountId != null)
                .Select(n => n.AccountId!.Value).Distinct().ToListAsync(ct)).ToHashSet();
            var allNoTpid = await _db.Accounts
                .Where(a => a.Tpid == null || a.Tpid == "")
                .OrderBy(a => a.AccountName)
                .ToListAsync(ct);
            var skippedReferenced = allNoTpid.Count(a => referenced.Contains(a.AccountId));
            var targets = allNoTpid.Where(a => !referenced.Contains(a.AccountId)).ToList();

            int parked = 0, noms = 0, links = 0, owns = 0, engs = 0, waves = 0, strat = 0, strategicFlagged = 0;
            foreach (var a in targets)
            {
                var resourceLinks = await _db.ResourceAccounts.Where(x => x.AccountId == a.AccountId)
                    .Select(x => new { x.ResourceId, x.RelationshipType }).ToListAsync(ct);
                var nomIds = await _db.Nominations.Where(x => x.AccountId == a.AccountId).Select(x => x.Id).ToListAsync(ct);
                var waveCount = await _db.WaveLinks.CountAsync(x => x.AccountId == a.AccountId, ct);
                var engCount = await _db.EngagementFacts.CountAsync(x => x.AccountId == a.AccountId, ct);
                var ownRows = await _db.OwnershipHistory.Where(x => x.AccountId == a.AccountId).ToListAsync(ct);
                var stratRows = await _db.StrategicAccountConfigurations.Where(x => x.AccountId == a.AccountId).ToListAsync(ct);

                noms += nomIds.Count; links += resourceLinks.Count; waves += waveCount;
                engs += engCount; owns += ownRows.Count; strat += stratRows.Count;
                if (a.StrategicFlag) strategicFlagged++;

                if (apply)
                {
                    _db.ParkedAccounts.Add(new ParkedAccount
                    {
                        OriginalAccountId = a.AccountId,
                        AccountName = a.AccountName,
                        Tpid = a.Tpid,
                        ExternalAccountId = a.ExternalAccountId,
                        Segment = a.Segment,
                        Region = a.Region,
                        Status = a.Status,
                        StrategicFlag = a.StrategicFlag,
                        PriorityWeight = a.PriorityWeight,
                        Aliases = a.Aliases,
                        ProjectManager = a.ProjectManager,
                        SolutionArchitect = a.SolutionArchitect,
                        Cftl = a.Cftl,
                        AccountOwner = a.AccountOwner,
                        CustomerPoc = a.CustomerPoc,
                        BackupOwner = a.BackupOwner,
                        ResourceLinks = resourceLinks.Count > 0
                            ? string.Join(";", resourceLinks.Select(r => $"{r.ResourceId}:{r.RelationshipType}"))
                            : null,
                        NominationIds = nomIds.Count > 0 ? string.Join(";", nomIds) : null,
                        Reason = "No TPID (not in FDO / master customer list)",
                        ParkedUtc = DateTimeOffset.UtcNow,
                    });

                    foreach (var n in await _db.Nominations.Where(x => x.AccountId == a.AccountId).ToListAsync(ct)) n.AccountId = null;
                    foreach (var w in await _db.WaveLinks.Where(x => x.AccountId == a.AccountId).ToListAsync(ct)) w.AccountId = null;
                    foreach (var e in await _db.EngagementFacts.Where(x => x.AccountId == a.AccountId).ToListAsync(ct)) e.AccountId = null;
                    _db.StrategicAccountConfigurations.RemoveRange(stratRows);
                    _db.OwnershipHistory.RemoveRange(ownRows);
                    _db.ResourceAccounts.RemoveRange(await _db.ResourceAccounts.Where(x => x.AccountId == a.AccountId).ToListAsync(ct));
                    _db.Accounts.Remove(a);
                }
                parked++;
            }

            if (apply && parked > 0)
            {
                await _db.SaveChangesAsync(ct);
                await SyncStrategicFlagsAsync(ct);
                await _capacityRebuild.RebuildAllAsync(ct);
            }
            result.Success = true;
            result.Messages.Add($"{(apply ? "Parked" : "Preview")}: {parked} no-TPID account(s) ({strategicFlagged} strategic-flagged); " +
                $"kept {skippedReferenced} referenced by a nomination (no orphans). " +
                $"Detached resourceLinks={links}, ownership={owns}, engagements={engs}, waves={waves}, strategicCfg={strat}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Park no-TPID accounts failed");
            result.Success = false;
            result.Messages.Add($"Park failed: {ex.Message}");
        }
        result.CompletedUtc = DateTimeOffset.UtcNow;
        return result;
    }

    // Restores every parked account into the master: re-creates the account, its resource links, and
    // re-points the nominations whose AccountId was nulled at park time. (Ownership/engagement/wave/strategic
    // rows detached at park time are not reconstructed.)
    public async Task<DataRefreshResultDto> UnparkAllAccountsAsync(CancellationToken ct = default)
    {
        var result = new DataRefreshResultDto { StartedUtc = DateTimeOffset.UtcNow };
        try
        {
            var parkedList = await _db.ParkedAccounts.ToListAsync(ct);
            int restored = 0, relinked = 0, renoms = 0;
            foreach (var p in parkedList)
            {
                var acct = new Account
                {
                    AccountName = p.AccountName,
                    Region = p.Region,
                    Status = p.Status,
                    StrategicFlag = p.StrategicFlag,
                    PriorityWeight = p.PriorityWeight,
                    Segment = p.Segment,
                    Tpid = p.Tpid,
                    ExternalAccountId = p.ExternalAccountId,
                    Aliases = p.Aliases,
                    ProjectManager = p.ProjectManager,
                    SolutionArchitect = p.SolutionArchitect,
                    Cftl = p.Cftl,
                    AccountOwner = p.AccountOwner,
                    CustomerPoc = p.CustomerPoc,
                    BackupOwner = p.BackupOwner,
                };
                _db.Accounts.Add(acct);
                await _db.SaveChangesAsync(ct); // materialize AccountId for the links/references below

                foreach (var token in (p.ResourceLinks ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = token.Split(':', 2);
                    if (int.TryParse(parts[0], out var rid))
                    {
                        _db.ResourceAccounts.Add(new ResourceAccount
                        {
                            ResourceId = rid,
                            AccountId = acct.AccountId,
                            RelationshipType = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : "Primary",
                            Source = "Unpark",
                        });
                        relinked++;
                    }
                }

                foreach (var token in (p.NominationIds ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(token, out var nid))
                    {
                        var nom = await _db.Nominations.FirstOrDefaultAsync(x => x.Id == nid, ct);
                        if (nom is not null && nom.AccountId is null) { nom.AccountId = acct.AccountId; renoms++; }
                    }
                }

                _db.ParkedAccounts.Remove(p);
                restored++;
            }

            if (restored > 0)
            {
                await _db.SaveChangesAsync(ct);
                await SyncStrategicFlagsAsync(ct);
                await _capacityRebuild.RebuildAllAsync(ct);
            }
            result.Success = true;
            result.Messages.Add($"Unparked {restored} account(s); restored resourceLinks={relinked}, re-pointed noms={renoms}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unpark accounts failed");
            result.Success = false;
            result.Messages.Add($"Unpark failed: {ex.Message}");
        }
        result.CompletedUtc = DateTimeOffset.UtcNow;
        return result;
    }

    // One-time enrichment from "Summary of All Offerings": matches nominations by TPID + Task Id and sets the
    // offering fields; updates Account.Segment by TPID. apply=false previews match/update counts only.
    public async Task<DataRefreshResultDto> ImportOfferingsAsync(Stream workbook, bool apply, CancellationToken ct = default)
    {
        var result = new DataRefreshResultDto { StartedUtc = DateTimeOffset.UtcNow };
        try
        {
            using var ms = new MemoryStream();
            await workbook.CopyToAsync(ms, ct);
            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.First();
            var rowsUsed = ws.RowsUsed().ToList();
            if (rowsUsed.Count < 2)
            {
                result.Success = true;
                result.Messages.Add("No data rows in the Offerings workbook.");
                result.CompletedUtc = DateTimeOffset.UtcNow;
                return result;
            }

            var h = ExcelHelpers.MapHeaders(rowsUsed[0]);
            var colTpid = ExcelHelpers.FindColumn(h, "TPID", "TP ID");
            var colTask = ExcelHelpers.FindColumn(h, "Task ID", "Task Id");
            var colSegment = ExcelHelpers.FindColumn(h, "Segment");
            var colPath = ExcelHelpers.FindColumn(h, "Primary Migration Path");
            var colPartner = ExcelHelpers.FindColumn(h, "Partner Name");
            var colCores = ExcelHelpers.FindColumn(h, "Total Cores");
            var colTool = ExcelHelpers.FindColumn(h, "Is Tool Attached");
            var colAuto = ExcelHelpers.FindColumn(h, "Is Automation Used");
            var colMode = ExcelHelpers.FindColumn(h, "Mode Of Access");
            var colTotalAcr = ExcelHelpers.FindColumn(h, "Total ACR");
            var colNnrAcr = ExcelHelpers.FindColumn(h, "NNR ACR");
            var colNominated = ExcelHelpers.FindColumn(h, "Nom. Created Date", "Nomination Created Date", "Account Nominated date");
            var colApprovalDate = ExcelHelpers.FindColumn(h, "Nom. Approval Date", "Nomination Approval Date");
            var colActualStart = ExcelHelpers.FindColumn(h, "Actual Start Date");
            var colActualEnd = ExcelHelpers.FindColumn(h, "Actual End Date");
            var colPlannedStart = ExcelHelpers.FindColumn(h, "Planned Start Date");
            var colPlannedEnd = ExcelHelpers.FindColumn(h, "Planned End Date");
            if (colTpid is null || colTask is null)
            {
                result.Success = false;
                result.Messages.Add("Required columns 'TPID' and/or 'Task ID' were not found in the workbook.");
                result.CompletedUtc = DateTimeOffset.UtcNow;
                return result;
            }

            // Nominations keyed by (TPID, Task Id); accounts by TPID.
            var noms = await _db.Nominations.Include(n => n.Account).ToListAsync(ct);
            var byKey = new Dictionary<(string Tpid, string Task), Nomination>();
            foreach (var n in noms.Where(x => !string.IsNullOrWhiteSpace(x.ExternalTaskId)))
                byKey.TryAdd(((n.Account?.Tpid ?? string.Empty).Trim(), n.ExternalTaskId!.Trim()), n);
            var taskOnly = noms.Where(x => !string.IsNullOrWhiteSpace(x.ExternalTaskId))
                .GroupBy(x => x.ExternalTaskId!.Trim()).ToDictionary(g => g.Key, g => g.First());
            var accountsByTpid = (await _db.Accounts.Where(a => a.Tpid != null && a.Tpid != "").ToListAsync(ct))
                .GroupBy(a => a.Tpid!.Trim()).ToDictionary(g => g.Key, g => g.First());
            var knownSegments = new HashSet<string>(await _db.Segments.Select(s => s.Name).ToListAsync(ct), StringComparer.OrdinalIgnoreCase);
            var maxSort = await _db.Segments.AnyAsync(ct) ? await _db.Segments.MaxAsync(s => s.SortOrder, ct) : 0;

            static bool? ParseYesNo(string? v) =>
                string.IsNullOrWhiteSpace(v) ? null
                : v.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase) ? true
                : v.Trim().Equals("No", StringComparison.OrdinalIgnoreCase) ? false
                : null;

            int nomMatched = 0, taskNotFound = 0, tpidMismatch = 0, segUpdated = 0, tpidNotInMaster = 0;
            var segByTpid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // first non-blank segment per TPID

            foreach (var row in rowsUsed.Skip(1))
            {
                if (ExcelHelpers.IsEmptyRow(row)) continue;
                var tpid = (ExcelHelpers.GetString(row, colTpid) ?? string.Empty).Trim();
                var task = (ExcelHelpers.GetString(row, colTask) ?? string.Empty).Trim();
                var segment = ExcelHelpers.GetString(row, colSegment);

                if (tpid.Length > 0 && !string.IsNullOrWhiteSpace(segment) && !segByTpid.ContainsKey(tpid))
                    segByTpid[tpid] = segment!;

                if (task.Length == 0) continue;
                if (!byKey.TryGetValue((tpid, task), out var nom))
                {
                    if (taskOnly.ContainsKey(task)) tpidMismatch++; else taskNotFound++;
                    continue;
                }
                nomMatched++;
                if (apply)
                {
                    nom.PrimaryMigrationPath = ExcelHelpers.GetString(row, colPath);
                    nom.PartnerName = ExcelHelpers.GetString(row, colPartner);
                    var coresStr = ExcelHelpers.GetString(row, colCores);
                    nom.TotalCores = coresStr is not null && double.TryParse(coresStr,
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cores)
                        ? (int)Math.Round(cores) : null;
                    nom.IsToolAttached = ParseYesNo(ExcelHelpers.GetString(row, colTool));
                    nom.IsAutomationUsed = ParseYesNo(ExcelHelpers.GetString(row, colAuto));
                    nom.ModeOfAccess = ExcelHelpers.GetString(row, colMode);
                    var totalAcrStr = ExcelHelpers.GetString(row, colTotalAcr);
                    nom.TotalAcr = totalAcrStr is not null && decimal.TryParse(totalAcrStr,
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tacr) ? tacr : null;
                    var nnrAcrStr = ExcelHelpers.GetString(row, colNnrAcr);
                    nom.NnrAcr = nnrAcrStr is not null && decimal.TryParse(nnrAcrStr,
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var nacr) ? nacr : null;
                    nom.NominatedDate = ParseExcelDate(row, colNominated);
                    nom.ApprovalDate = ParseExcelDate(row, colApprovalDate);
                    nom.ActualStartDate = ParseExcelDate(row, colActualStart);
                    nom.ActualEndDate = ParseExcelDate(row, colActualEnd);
                    nom.PlannedStartDate = ParseExcelDate(row, colPlannedStart);
                    nom.PlannedEndDate = ParseExcelDate(row, colPlannedEnd);
                    nom.TotalDays = nom.ActualStartDate is { } ds && nom.ActualEndDate is { } de && de >= ds
                        ? de.DayNumber - ds.DayNumber : null;
                }
            }

            foreach (var (tpid, segment) in segByTpid)
            {
                if (!accountsByTpid.TryGetValue(tpid, out var acct)) { tpidNotInMaster++; continue; }
                if (!string.Equals(acct.Segment, segment, StringComparison.OrdinalIgnoreCase))
                {
                    segUpdated++;
                    if (apply)
                    {
                        acct.Segment = segment;
                        if (knownSegments.Add(segment))
                            _db.Segments.Add(new SegmentConfiguration { Name = segment, SortOrder = ++maxSort });
                    }
                }
            }

            if (apply)
                await _db.SaveChangesAsync(ct);
            result.Success = true;
            result.Messages.Add($"{(apply ? "Applied" : "Preview")}: nominations matched (TPID+TaskId)={nomMatched}, " +
                $"task-id not found={taskNotFound}, tpid-mismatch={tpidMismatch}; account Segment updates={segUpdated}, " +
                $"tpid not in master={tpidNotInMaster}. Data rows={rowsUsed.Count - 1}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Offerings import failed");
            result.Success = false;
            result.Messages.Add($"Offerings import failed: {ex.Message}");
        }
        result.CompletedUtc = DateTimeOffset.UtcNow;
        return result;
    }

    // One-time import of DE-Completed: adds finished migrations as Completed nominations (or marks a matching
    // in-flight row Completed), with nominated/start/end dates + duration. apply=false previews counts.
    public async Task<DataRefreshResultDto> ImportCompletedAsync(Stream workbook, bool apply, CancellationToken ct = default)
    {
        var result = new DataRefreshResultDto { StartedUtc = DateTimeOffset.UtcNow };
        try
        {
            using var ms = new MemoryStream();
            await workbook.CopyToAsync(ms, ct);
            ms.Position = 0;
            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.First();
            var rowsUsed = ws.RowsUsed().ToList();
            if (rowsUsed.Count < 2)
            {
                result.Success = true;
                result.Messages.Add("No data rows in the Completed workbook.");
                result.CompletedUtc = DateTimeOffset.UtcNow;
                return result;
            }

            var h = ExcelHelpers.MapHeaders(rowsUsed[0]);
            var colTpid = ExcelHelpers.FindColumn(h, "TPID", "TP ID");
            var colTask = ExcelHelpers.FindColumn(h, "Task Id", "Task ID");
            var colCustomer = ExcelHelpers.FindColumn(h, "Customer Name", "Customer");
            var colOffering = ExcelHelpers.FindColumn(h, "Offering Name and Phase", "Offering");
            var colNominated = ExcelHelpers.FindColumn(h, "Account Nominated date", "Nom. Created Date");
            var colStart = ExcelHelpers.FindColumn(h, "Offering Start date", "Actual Start Date");
            var colMigration = ExcelHelpers.FindColumn(h, "Migration Status");
            var colTotalDays = ExcelHelpers.FindColumn(h, "Total Days");
            var colCurrent = ExcelHelpers.FindColumn(h, "Current State");
            var colRegion = ExcelHelpers.FindColumn(h, "Region");
            var colSa = ExcelHelpers.FindColumn(h, "Solution Architect Hierarchy - Solution Architect", "Solution Architect");
            var colCftl = ExcelHelpers.FindColumn(h, "CFTL Primary Hierarchy - CFTL Primary", "CFTL Primary");
            var colPc = ExcelHelpers.FindColumn(h, "Project Coordinator Hierarchy - Project Coordinator", "Project Coordinator");
            if (colTpid is null || colTask is null)
            {
                result.Success = false;
                result.Messages.Add("Required columns 'TPID' and/or 'Task Id' were not found in the workbook.");
                result.CompletedUtc = DateTimeOffset.UtcNow;
                return result;
            }

            var noms = await _db.Nominations.ToListAsync(ct);
            var byTask = new Dictionary<string, Nomination>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in noms.Where(x => !string.IsNullOrWhiteSpace(x.ExternalTaskId)))
                byTask.TryAdd(n.ExternalTaskId!.Trim(), n);
            var accountsByTpid = (await _db.Accounts.Where(a => a.Tpid != null && a.Tpid != "").ToListAsync(ct))
                .GroupBy(a => a.Tpid!.Trim()).ToDictionary(g => g.Key, g => g.First());

            int created = 0, enriched = 0, accountsCreated = 0, skipped = 0;
            foreach (var row in rowsUsed.Skip(1))
            {
                if (ExcelHelpers.IsEmptyRow(row)) continue;
                var task = (ExcelHelpers.GetString(row, colTask) ?? string.Empty).Trim();
                var tpid = (ExcelHelpers.GetString(row, colTpid) ?? string.Empty).Trim();
                if (task.Length == 0) { skipped++; continue; }

                var nominated = ParseExcelDate(row, colNominated);
                var start = ParseExcelDate(row, colStart);
                var totalDaysStr = ExcelHelpers.GetString(row, colTotalDays);
                int? totalDays = totalDaysStr is not null && double.TryParse(totalDaysStr,
                    System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var td)
                    ? (int)Math.Round(td) : null;
                var end = start is { } s && totalDays is { } d ? s.AddDays(d) : (DateOnly?)null;

                if (byTask.TryGetValue(task, out var existingNom))
                {
                    enriched++;
                    if (apply)
                    {
                        existingNom.Status = NominationStatusType.Completed;
                        existingNom.NominatedDate ??= nominated;
                        existingNom.ActualStartDate ??= start;
                        existingNom.ActualEndDate ??= end;
                        existingNom.TotalDays ??= totalDays;
                    }
                    continue;
                }

                created++;
                if (apply)
                {
                    var customer = ExcelHelpers.GetString(row, colCustomer) ?? (tpid.Length > 0 ? $"TPID {tpid}" : "Unknown");
                    var region = NormRegion(ExcelHelpers.GetString(row, colRegion));
                    accountsByTpid.TryGetValue(tpid, out var acct);
                    if (acct is null && tpid.Length > 0)
                    {
                        acct = new Account { AccountName = customer, Tpid = tpid, Region = region, Status = "Active" };
                        _db.Accounts.Add(acct);
                        await _db.SaveChangesAsync(ct);
                        accountsByTpid[tpid] = acct;
                        accountsCreated++;
                    }
                    _db.Nominations.Add(new Nomination
                    {
                        ExternalTaskId = task,
                        AccountId = acct?.AccountId,
                        AccountName = acct?.AccountName ?? customer,
                        Region = region,
                        Status = NominationStatusType.Completed,
                        ApprovalStatus = "Approved",
                        Technology = ExcelHelpers.GetString(row, colOffering),
                        MigrationStatus = ExcelHelpers.GetString(row, colMigration),
                        CurrentState = ExcelHelpers.GetString(row, colCurrent),
                        SolutionArchitect = ExcelHelpers.GetString(row, colSa),
                        CftlPrimary = ExcelHelpers.GetString(row, colCftl),
                        ProjectCoordinator = ExcelHelpers.GetString(row, colPc),
                        OpenedDate = nominated ?? DateOnly.FromDateTime(DateTime.UtcNow),
                        NominatedDate = nominated,
                        ActualStartDate = start,
                        ActualEndDate = end,
                        TotalDays = totalDays,
                    });
                }
            }

            if (apply)
                await _db.SaveChangesAsync(ct);
            result.Success = true;
            result.Messages.Add($"{(apply ? "Applied" : "Preview")}: completed created={created} (accounts created={accountsCreated}), " +
                $"matched marked completed={enriched}, skipped={skipped}. Data rows={rowsUsed.Count - 1}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Completed import failed");
            result.Success = false;
            result.Messages.Add($"Completed import failed: {ex.Message}");
        }
        result.CompletedUtc = DateTimeOffset.UtcNow;
        return result;
    }

    private static string NormRegion(string? r)
    {
        var t = (r ?? string.Empty).ToUpperInvariant();
        if (t.Contains("EMEA")) return "EMEA";
        if (t.Contains("ASIA")) return "ASIA";
        if (t.Contains("LATAM")) return "LATAM";
        if (t.Contains("AMER") || t.Contains("NORTH AMERICA")) return "AMER";
        return "UNSPECIFIED";
    }

    private static DateOnly? ParseExcelDate(IXLRow row, int? column)
    {
        if (column is null) return null;
        var cell = row.Cell(column.Value);
        if (cell.TryGetValue<DateTime>(out var dt)) return DateOnly.FromDateTime(dt);
        var s = cell.GetString().Trim();
        if (string.IsNullOrEmpty(s)) return null;
        if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var serial) && serial is > 20000 and < 80000)
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
            return DateOnly.FromDateTime(parsed);
        return null;
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
