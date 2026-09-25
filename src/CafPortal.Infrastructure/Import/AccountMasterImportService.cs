using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CafPortal.Infrastructure.Import;

/// <summary>
/// Loads the account master list ("Nominations In-Flight.xlsx": Segment, TPID, Customer Name, Account ID).
/// One row per in-flight nomination, so it deduplicates to one record per account (keyed by TPID) and upserts the
/// canonical name + segment. Additive only — never deletes accounts and never touches ownership / region / status.
/// </summary>
public class AccountMasterImportService(AppDbContext db, ILogger<AccountMasterImportService> logger) : IAccountMasterImportService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<AccountMasterImportService> _logger = logger;

    public async Task<int> ImportAsync(Stream workbook, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(workbook);
        var ws = wb.Worksheets.First();
        var rows = ws.RowsUsed().ToList();
        if (rows.Count < 2)
            return 0;

        var h = ExcelHelpers.MapHeaders(rows[0]);
        var colSegment = ExcelHelpers.FindColumn(h, "Segment");
        var colTpid = ExcelHelpers.FindColumn(h, "TPID", "TP ID");
        var colCustomer = ExcelHelpers.FindColumn(h, "Customer Name", "Customer", "Account Name");
        var colExtId = ExcelHelpers.FindColumn(h, "Account ID", "AccountId");
        if (colCustomer is null && colTpid is null)
            return 0;

        // Deduplicate to one canonical record per account (TPID → Account ID → name).
        var byKey = new Dictionary<string, (string Name, string? Tpid, string? ExtId, string? Segment)>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;
            var name = ExcelHelpers.GetString(row, colCustomer);
            var tpid = ExcelHelpers.GetString(row, colTpid);
            var extId = ExcelHelpers.GetString(row, colExtId);
            var segment = ExcelHelpers.GetString(row, colSegment);
            var key = tpid ?? extId ?? name;
            if (key is null)
                continue;
            byKey[key] = (name ?? key, tpid, extId, segment);
        }

        var accounts = await _db.Accounts.ToListAsync(ct);
        // Alias-aware name matching so a master "Customer Name" that is a known variant merges instead of duplicating.
        var aliasMap = await _db.AccountAliases.AsNoTracking()
            .ToDictionaryAsync(a => a.Alias, a => a.StandardAccountName, StringComparer.OrdinalIgnoreCase, ct);
        string Canon(string name)
        {
            var trimmed = name.Trim();
            return aliasMap.TryGetValue(trimmed, out var std) ? std : trimmed;
        }

        var byTpid = accounts.Where(a => !string.IsNullOrWhiteSpace(a.Tpid))
            .GroupBy(a => a.Tpid!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var byExt = accounts.Where(a => !string.IsNullOrWhiteSpace(a.ExternalAccountId))
            .GroupBy(a => a.ExternalAccountId!, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var byName = accounts.GroupBy(a => Canon(a.AccountName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var knownSegments = new HashSet<string>(await _db.Segments.Select(s => s.Name).ToListAsync(ct), StringComparer.OrdinalIgnoreCase);
        var maxSort = await _db.Segments.AnyAsync(ct) ? await _db.Segments.MaxAsync(s => s.SortOrder, ct) : 0;

        var upserts = 0;
        foreach (var (_, rec) in byKey)
        {
            Account? acct = null;
            if (rec.Tpid is not null) byTpid.TryGetValue(rec.Tpid, out acct);
            if (acct is null && rec.ExtId is not null) byExt.TryGetValue(rec.ExtId, out acct);
            if (acct is null) byName.TryGetValue(Canon(rec.Name), out acct);

            if (acct is null)
            {
                acct = new Account
                {
                    AccountName = rec.Name,
                    Region = "UNSPECIFIED", // region isn't in the master file; enriched by other imports
                    Status = "Active",
                    Tpid = rec.Tpid,
                    ExternalAccountId = rec.ExtId,
                    Segment = rec.Segment,
                };
                _db.Accounts.Add(acct);
                if (rec.Tpid is not null) byTpid[rec.Tpid] = acct;
            }
            else
            {
                acct.AccountName = rec.Name;
                if (rec.Tpid is not null) acct.Tpid = rec.Tpid;
                if (rec.ExtId is not null) acct.ExternalAccountId = rec.ExtId;
                if (!string.IsNullOrWhiteSpace(rec.Segment)) acct.Segment = rec.Segment;
                acct.UpdatedUtc = DateTimeOffset.UtcNow;
            }

            // Keep the segment vocabulary in sync so new segments are selectable on the Accounts page.
            if (!string.IsNullOrWhiteSpace(rec.Segment) && knownSegments.Add(rec.Segment))
                _db.Segments.Add(new SegmentConfiguration { Name = rec.Segment, SortOrder = ++maxSort });

            upserts++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Account master import: {Count} distinct accounts upserted", upserts);
        return upserts;
    }
}
