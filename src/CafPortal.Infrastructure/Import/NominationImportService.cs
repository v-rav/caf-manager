using System.Globalization;
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
        var colExtId = ExcelHelpers.FindColumn(h, "Account ID", "AccountId");
        var colCustomer = ExcelHelpers.FindColumn(h, "Customer Name", "Customer", "Account Name");
        var colOffering = ExcelHelpers.FindColumn(h, "Offering Name", "Offering", "Wave");
        var colNominated = ExcelHelpers.FindColumn(h, "Nominated date", "Nominated", "Wave Created");
        var colMigration = ExcelHelpers.FindColumn(h, "Migration Status");
        var colApproval = ExcelHelpers.FindColumn(h, "Approval Status", "Nomination Approval");
        var colRegion = ExcelHelpers.FindColumn(h, "Region", "Area");
        var colSummary = ExcelHelpers.FindColumn(h, "Status Summary");
        var colNextAction = ExcelHelpers.FindColumn(h, "Next Best Action");
        var colCurrentState = ExcelHelpers.FindColumn(h, "Current State");
        var colSolutionArchitect = ExcelHelpers.FindColumn(h, "Solution Architect Hierarchy - Solution Architect", "Solution Architect");
        var colCftl = ExcelHelpers.FindColumn(h, "CFTL Primary Hierarchy - CFTL Primary", "CFTL Primary");
        var colProjectCoordinator = ExcelHelpers.FindColumn(h, "Project Coordinator Hierarchy - Project Coordinator", "Project Coordinator");

        var resolver = new AccountResolver(_db);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var imported = 0;

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

            var migration = Clean(ExcelHelpers.GetString(row, colMigration));
            var approval = Clean(ExcelHelpers.GetString(row, colApproval));
            var opened = ParseDate(row, colNominated) ?? DateOnly.FromDateTime(DateTime.UtcNow);

            _db.Nominations.Add(new Nomination
            {
                AccountId = account.AccountId,
                AccountName = account.AccountName,
                Technology = offering,
                Region = region == "UNSPECIFIED" ? account.Region : region,
                Status = MapStatus(migration, approval),
                OpenedDate = opened,
                Remarks = BuildRemarks(approval, migration,
                    ExcelHelpers.GetString(row, colSummary) ?? ExcelHelpers.GetString(row, colNextAction)),
                MigrationStatus = migration,
                CurrentState = Truncate(Clean(ExcelHelpers.GetString(row, colCurrentState)), 2000),
                SolutionArchitect = Clean(ExcelHelpers.GetString(row, colSolutionArchitect)),
                CftlPrimary = Clean(ExcelHelpers.GetString(row, colCftl)),
                ProjectCoordinator = Clean(ExcelHelpers.GetString(row, colProjectCoordinator))
            });
            imported++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("NominationImportService imported {Count} nominations", imported);
        return imported;
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
