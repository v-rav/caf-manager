using System.Globalization;
using System.Text.Json;
using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Enums;
using CafPortal.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CafPortal.Infrastructure.Import;

/// <summary>
/// Loads the nomination pipeline export ("Detail View.xlsx"). Each row is a customer offering/wave.
/// Uses the official Customer Name (keyed by TPID) as the account master, and creates one nomination per row.
/// </summary>
public class NominationImportService(AppDbContext db, ILogger<NominationImportService> logger) : INominationImportService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<NominationImportService> _logger = logger;

    private static readonly string[] DateFormats =
    [
        "dd-MM-yyyy HH:mm:ss", "dd-MM-yyyy", "d-M-yyyy HH:mm:ss", "d-M-yyyy",
        "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"
    ];

    public async Task<int> ImportAsync(Stream workbook, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(workbook);
        var ws = wb.Worksheets.First();
        var rows = ws.RowsUsed().ToList();
        if (rows.Count < 2)
            return 0;

        var h = ExcelHelpers.MapHeaders(rows[0]);
        var colTpid = ExcelHelpers.FindColumn(h, "TPID", "TP ID");
        var colTaskId = ExcelHelpers.FindColumn(h, "Task Id", "TaskId");
        var colExtId = ExcelHelpers.FindColumn(h, "Account ID", "AccountId");
        var colCustomer = ExcelHelpers.FindColumn(h, "Customer Name", "Customer", "Account Name");
        var colOffering = ExcelHelpers.FindColumn(h, "Offering Name", "Offering", "Wave");
        var colNominated = ExcelHelpers.FindColumn(h, "Nominated date", "Nominated", "Wave Created");
        var colMigration = ExcelHelpers.FindColumn(h, "Migration Status");
        var colStage1 = ExcelHelpers.FindColumn(h, "Validating & Initial Scope", "1 - Validating");
        var colStage2 = ExcelHelpers.FindColumn(h, "Executing Pre-Requisites", "2 - Executing Pre");
        var colStage3 = ExcelHelpers.FindColumn(h, "Finalize Scope", "3 - Finalize");
        var colStage4 = ExcelHelpers.FindColumn(h, "Executing Migration", "4 - Executing Migration");
        var colApproval = ExcelHelpers.FindColumn(h, "Approval Status", "Nomination Approval");
        var colRegion = ExcelHelpers.FindColumn(h, "Region", "Area");
        var colSummary = ExcelHelpers.FindColumn(h, "Status Summary");
        var colNextAction = ExcelHelpers.FindColumn(h, "Next Best Action");
        var colCurrentState = ExcelHelpers.FindColumn(h, "Current State");
        var colSolutionArchitect = ExcelHelpers.FindColumn(h, "Solution Architect Hierarchy - Solution Architect", "Solution Architect");
        var colCftl = ExcelHelpers.FindColumn(h, "CFTL Primary Hierarchy - CFTL Primary", "CFTL Primary");
        var colProjectCoordinator = ExcelHelpers.FindColumn(h, "Project Coordinator Hierarchy - Project Coordinator", "Project Coordinator");
        // Wave linkage: "Linked to" (names) + "Linked to ID" (ids), each a ';'-separated list of related waves.
        var colLinkedId = ExcelHelpers.FindColumn(h, "Linked to ID", "Linked to Id");
        int? colLinkedTo = null;
        foreach (var kv in h)
            if (kv.Value != colLinkedId && kv.Key.Contains("Linked to", StringComparison.OrdinalIgnoreCase)) { colLinkedTo = kv.Value; break; }

        var resolver = new AccountResolver(_db);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenTaskIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // FDO drops are full snapshots keyed by Task Id — preload for upsert-merge.
        var existing = await _db.Nominations.Include(n => n.WaveLinks).ToListAsync(ct);
        var byTaskId = existing.Where(n => !string.IsNullOrWhiteSpace(n.ExternalTaskId))
            .GroupBy(n => n.ExternalTaskId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var imported = 0;
        var run = new ImportRun { StartedUtc = DateTime.UtcNow, Source = "nominations" };
        var added = 0; var updated = 0; var unchanged = 0;

        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;

            var customer = Clean(ExcelHelpers.GetString(row, colCustomer));
            if (customer is null)
                continue;

            var region = NormalizeRegion(ExcelHelpers.GetString(row, colRegion));
            var tpid = Clean(ExcelHelpers.GetString(row, colTpid));
            var extId = Clean(ExcelHelpers.GetString(row, colExtId));

            // Account master: official customer name, keyed by TPID.
            var account = await ResolveAccountAsync(resolver, customer, region, tpid, extId, ct);
            await _db.SaveChangesAsync(ct); // ensure account id

            var offering = CleanOffering(ExcelHelpers.GetString(row, colOffering));
            // De-dupe identical rows within the file (same account + offering).
            if (!seen.Add($"{account.AccountId}|{offering}"))
                continue;

            var taskId = Clean(ExcelHelpers.GetString(row, colTaskId));
            var migration = Clean(ExcelHelpers.GetString(row, colMigration));
            var stageDays = new int?[]
            {
                ParseDays(ExcelHelpers.GetString(row, colStage1)),
                ParseDays(ExcelHelpers.GetString(row, colStage2)),
                ParseDays(ExcelHelpers.GetString(row, colStage3)),
                ParseDays(ExcelHelpers.GetString(row, colStage4)),
            };
            var stageIdx = StageIndex(migration);
            var stageAge = stageIdx is int si ? stageDays[si - 1] : null;
            var approval = Clean(ExcelHelpers.GetString(row, colApproval));
            var opened = ParseDate(row, colNominated) ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var regionValue = region == "UNSPECIFIED" ? account.Region : region;

            // Match by stable Task Id; fall back to (account + offering) to adopt legacy rows once.
            Nomination? nom = null;
            if (taskId is not null && byTaskId.TryGetValue(taskId, out var byId))
                nom = byId;
            nom ??= existing.FirstOrDefault(n => string.IsNullOrWhiteSpace(n.ExternalTaskId)
                && n.AccountId == account.AccountId
                && string.Equals(n.Technology, offering, StringComparison.OrdinalIgnoreCase));

            var isNew = nom is null;
            var before = isNew ? null : Snapshot(nom!);

            if (isNew)
            {
                nom = new Nomination { OpenedDate = opened, Status = MapStatus(migration, approval) };
                _db.Nominations.Add(nom);
                existing.Add(nom);
            }

            if (taskId is not null)
            {
                nom!.ExternalTaskId = taskId;
                byTaskId[taskId] = nom;
                seenTaskIds.Add(taskId);
            }

            // FDO-owned fields — refreshed every drop.
            nom!.AccountId = account.AccountId;
            nom.AccountName = account.AccountName;
            nom.Technology = offering;
            nom.Region = regionValue;
            nom.MigrationStatus = migration;
            nom.ApprovalStatus = approval;
            nom.StageAgeDays = stageAge;
            nom.CurrentState = Truncate(Clean(ExcelHelpers.GetString(row, colCurrentState)), 2000);
            // Revive: a row present in this drop was wrongly withdrawn (e.g. absent from an earlier partial
            // drop). Un-withdraw it symmetrically; MapStatus keeps FDO-declined/settled rows settled.
            if (!isNew && nom.Status == NominationStatusType.Withdrawn)
                nom.Status = MapStatus(migration, approval);
            // Ownership (PM/CFTL/SA) is portal-owned once set: seed from FDO only when empty.
            if (string.IsNullOrWhiteSpace(nom.SolutionArchitect))
                nom.SolutionArchitect = Clean(ExcelHelpers.GetString(row, colSolutionArchitect));
            if (string.IsNullOrWhiteSpace(nom.CftlPrimary))
                nom.CftlPrimary = Clean(ExcelHelpers.GetString(row, colCftl));
            if (string.IsNullOrWhiteSpace(nom.ProjectCoordinator))
                nom.ProjectCoordinator = Clean(ExcelHelpers.GetString(row, colProjectCoordinator));
            // Portal-owned (Status, BlockedReason, BlockedSince, FollowUpDate, WaveLinks) preserved.
            // Wave links: refresh FDO-sourced links from "Linked to"; keep portal-added links untouched.
            SyncFdoWaveLinks(nom!, account.AccountId,
                ExcelHelpers.GetString(row, colLinkedTo),
                ExcelHelpers.GetString(row, colLinkedId));
            // Remarks: seed from FDO on first insert; keep portal edits thereafter.
            if (string.IsNullOrWhiteSpace(nom.Remarks))
                nom.Remarks = BuildRemarks(approval, migration,
                    ExcelHelpers.GetString(row, colSummary) ?? ExcelHelpers.GetString(row, colNextAction));

            var label = $"{account.AccountName} \u00b7 {offering}";
            if (isNew)
            {
                run.Changes.Add(new ImportChange { ExternalKey = taskId, Label = label, ChangeType = "Added" });
                added++;
            }
            else
            {
                var diffs = DiffFields(before!, nom);
                if (diffs is not null)
                {
                    run.Changes.Add(new ImportChange { ExternalKey = nom.ExternalTaskId, Label = label, ChangeType = "Updated", ChangedFields = diffs });
                    updated++;
                }
                else unchanged++;
            }

            imported++;
        }

        await _db.SaveChangesAsync(ct);

        // Missing from this drop → soft-withdraw (preserve history + waves; never hard-delete).
        var withdrawnCount = 0;
        foreach (var n in existing.Where(n => !string.IsNullOrWhiteSpace(n.ExternalTaskId)
            && !seenTaskIds.Contains(n.ExternalTaskId!)
            && n.Status is not NominationStatusType.Withdrawn and not NominationStatusType.Closed
            and not NominationStatusType.Completed and not NominationStatusType.CustomerDeferred))
        {
            n.Status = NominationStatusType.Withdrawn;
            run.Changes.Add(new ImportChange { ExternalKey = n.ExternalTaskId, Label = n.AccountName, ChangeType = "Withdrawn" });
            withdrawnCount++;
        }
        if (withdrawnCount > 0)
            await _db.SaveChangesAsync(ct);

        // Record the import run + its changes for the History page.
        run.Added = added;
        run.Updated = updated;
        run.Withdrawn = withdrawnCount;
        run.Unchanged = unchanged;
        run.CompletedUtc = DateTime.UtcNow;
        _db.ImportRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        // Retention: prune import history beyond the window (nominations are never hard-deleted here).
        var cutoff = DateTime.UtcNow.AddDays(-HistoryRetentionDays);
        await _db.ImportRuns.Where(r => r.StartedUtc < cutoff).ExecuteDeleteAsync(ct);

        _logger.LogInformation("NominationImportService: {Added} added, {Updated} updated, {Unchanged} unchanged, {Withdrawn} withdrawn",
            added, updated, unchanged, withdrawnCount);
        return imported;
    }

    private const int HistoryRetentionDays = 90;

    private sealed record FdoSnapshot(string? Mig, string? State, int? Age, string? Sa, string? Cftl, string? Pm, string? Region, string? Tech, string? Name);

    private static FdoSnapshot Snapshot(Nomination n) =>
        new(n.MigrationStatus, n.CurrentState, n.StageAgeDays, n.SolutionArchitect, n.CftlPrimary, n.ProjectCoordinator, n.Region, n.Technology, n.AccountName);

    private static string? DiffFields(FdoSnapshot b, Nomination a)
    {
        var diffs = new List<object>();
        void D(string f, string? from, string? to) { if (!string.Equals(from, to, StringComparison.Ordinal)) diffs.Add(new { field = f, from, to }); }
        D("Migration Status", b.Mig, a.MigrationStatus);
        D("Current State", b.State, a.CurrentState);
        D("Stage Age (days)", b.Age?.ToString(), a.StageAgeDays?.ToString());
        D("Solution Architect", b.Sa, a.SolutionArchitect);
        D("CFTL", b.Cftl, a.CftlPrimary);
        D("Project Coordinator", b.Pm, a.ProjectCoordinator);
        D("Region", b.Region, a.Region);
        D("Offering", b.Tech, a.Technology);
        D("Account Name", b.Name, a.AccountName);
        return diffs.Count == 0 ? null : JsonSerializer.Serialize(diffs);
    }

    private async Task<Account> ResolveAccountAsync(AccountResolver resolver, string customer, string region,
        string? tpid, string? extId, CancellationToken ct)
    {
        Account? account = null;
        if (!string.IsNullOrWhiteSpace(tpid))
            account = _db.Accounts.Local.FirstOrDefault(a => a.Tpid == tpid)
                ?? await _db.Accounts.FirstOrDefaultAsync(a => a.Tpid == tpid, ct);

        // Fall back to name resolution (alias-aware) so this reconciles with accounts from the resource import.
        account ??= await resolver.ResolveAsync(customer, region, ct);

        // The Detail View carries the authoritative name and master keys.
        account.AccountName = customer;
        if (!string.IsNullOrWhiteSpace(tpid))
            account.Tpid = tpid;
        if (!string.IsNullOrWhiteSpace(extId))
            account.ExternalAccountId = extId;
        if (region != "UNSPECIFIED")
            account.Region = region;
        account.Status ??= "Active";
        account.UpdatedUtc = DateTimeOffset.UtcNow;
        return account;
    }

    private static NominationStatusType MapStatus(string? migration, string? approval)
    {
        if (string.Equals(approval, "Declined", StringComparison.OrdinalIgnoreCase))
            return NominationStatusType.Closed;

        var m = migration?.ToLowerInvariant() ?? string.Empty;
        if (m.Contains("executing") || m.Contains("finalize"))
            return NominationStatusType.InProgress;
        return NominationStatusType.Open;
    }

    // Migration Status text → numeric stage (1–4) of the migration journey.
    private static int? StageIndex(string? migration)
    {
        var m = migration?.ToLowerInvariant() ?? string.Empty;
        if (m.Contains("validating")) return 1;
        if (m.Contains("pre-requisite") || m.Contains("pre requisite") || m.Contains("prerequisite")) return 2;
        if (m.Contains("finalize")) return 3;
        if (m.Contains("executing migration")) return 4;
        return null;
    }

    // Parses a leading integer out of a "N days" stage cell.
    private static int? ParseDays(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(text, "-?\\d+");
        return match.Success && int.TryParse(match.Value, out var n) ? n : null;
    }

    private static string BuildRemarks(string? approval, string? migration, string? detail)
    {
        var prefix = string.Join(" · ", new[] { approval, migration }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var text = string.IsNullOrWhiteSpace(detail) ? prefix : $"{prefix}. {detail}";
        return text.Length > 1000 ? text[..1000] : text;
    }

    private static DateOnly? ParseDate(IXLRow row, int? column)
    {
        if (column is null)
            return null;
        var cell = row.Cell(column.Value);
        if (cell.TryGetValue<DateTime>(out var dt))
            return DateOnly.FromDateTime(dt);
        var text = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return DateOnly.FromDateTime(parsed);
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback)
            ? DateOnly.FromDateTime(fallback)
            : null;
    }

    private static string NormalizeRegion(string? raw)
    {
        var value = Clean(raw);
        if (string.IsNullOrWhiteSpace(value))
            return "UNSPECIFIED";
        var primary = value.Split([' ', '-', ',', '/', '&'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? value;
        return primary.ToUpperInvariant() switch
        {
            "US" or "USA" or "AMER" or "AMERICAS" or "NA" => "AMER",
            "EMEA" => "EMEA",
            "ASIA" or "APAC" => "ASIA",
            _ => primary.ToUpperInvariant()
        };
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var cleaned = value.Replace("\u200b", string.Empty).Replace("\u00a0", " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string? Truncate(string? value, int max)
        => value is not null && value.Length > max ? value[..max] : value;

    // Splits a ';'-separated FDO list (e.g. "Security Nominations - Wave 2; SQL Migration - Wave 1").
    private static List<string> SplitList(string? s)
        => string.IsNullOrWhiteSpace(s)
            ? new List<string>()
            : s.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    // Maps an FDO wave name to a hygiene wave type; App/self and unknown names return null (not tracked).
    private static WaveType? ClassifyWave(string name)
    {
        var t = name.ToLowerInvariant();
        if (t.Contains("security")) return WaveType.Security;
        if (t.Contains("sql") || t.Contains("ossdb") || t.Contains("database") || t.Contains("db migration")) return WaveType.Db;
        if (t.Contains("landing zone") || t.Contains("alz")) return WaveType.LandingZone;
        if (t.Contains("dispatch")) return WaveType.Dispatch;
        return null;
    }

    // Reconciles FDO-sourced wave links from the "Linked to" / "Linked to ID" columns; portal links are left as-is.
    private void SyncFdoWaveLinks(Nomination nom, int accountId, string? linkedNames, string? linkedIds)
    {
        var names = SplitList(linkedNames);
        var ids = SplitList(linkedIds);
        var desired = new List<(WaveType Type, string Reference, string? Note)>();
        for (var i = 0; i < names.Count; i++)
        {
            var wt = ClassifyWave(names[i]);
            if (wt is null) continue;
            var reference = Truncate(names[i], 300)!;
            var note = i < ids.Count ? Truncate(ids[i], 1000) : null;
            if (!desired.Any(d => d.Type == wt.Value && string.Equals(d.Reference, reference, StringComparison.OrdinalIgnoreCase)))
                desired.Add((wt.Value, reference, note));
        }

        foreach (var w in nom.WaveLinks.Where(w => w.Source == "FDO").ToList())
            if (!desired.Any(d => d.Type == w.WaveType && string.Equals(d.Reference, w.Reference, StringComparison.OrdinalIgnoreCase)))
            {
                nom.WaveLinks.Remove(w);
                _db.WaveLinks.Remove(w);
            }

        foreach (var d in desired)
            if (!nom.WaveLinks.Any(w => w.Source == "FDO" && w.WaveType == d.Type && string.Equals(w.Reference, d.Reference, StringComparison.OrdinalIgnoreCase)))
                nom.WaveLinks.Add(new WaveLink { WaveType = d.Type, Reference = d.Reference, Notes = d.Note, Source = "FDO", AccountId = accountId });
    }

    /// <summary>Drops the boilerplate "App Modernization Nominations - " prefix so the wave shows on its own.</summary>
    private static string CleanOffering(string? raw)
    {
        var value = Clean(raw);
        if (string.IsNullOrWhiteSpace(value))
            return "Nomination";
        const string prefix = "App Modernization Nominations -";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            value = value[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(value) ? "Nomination" : value;
    }
}
