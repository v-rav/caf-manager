using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using CafPortal.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace CafPortal.Infrastructure.Import;

/// <summary>Loads "Time-hunt_Tracking.xlsx" producing EngagementFact rows (customer meetings / demand signals).</summary>
public class EngagementImportService(AppDbContext db, ILogger<EngagementImportService> logger) : IEngagementImportService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<EngagementImportService> _logger = logger;

    public async Task<int> ImportAsync(Stream workbook, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(workbook);
        var ws = wb.Worksheets.First();
        var rows = ws.RowsUsed().ToList();
        if (rows.Count < 2)
            return 0;

        var lookup = await ResourceLookup.BuildAsync(_db, ct);
        var resolver = new AccountResolver(_db);
        var headers = ExcelHelpers.MapHeaders(rows[0]);

        var colDate = ExcelHelpers.FindColumn(headers, "Date", "Meeting Date");
        var colPsid = ExcelHelpers.FindColumn(headers, "PSID", "PS ID");
        var colEmail = ExcelHelpers.FindColumn(headers, "Email");
        var colResource = ExcelHelpers.FindColumn(headers, "Resource", "Name", "Owner");
        var colAccount = ExcelHelpers.FindColumn(headers, "Account", "Customer", "Client");
        var colMeeting = ExcelHelpers.FindColumn(headers, "Meeting", "Activity", "Subject", "Topic");
        var colDuration = ExcelHelpers.FindColumn(headers, "Duration", "Hours");
        var colRegion = ExcelHelpers.FindColumn(headers, "Region", "Geo");
        var colRemarks = ExcelHelpers.FindColumn(headers, "Remarks", "Notes", "Comments");

        var inserted = 0;
        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;

            var date = ExcelHelpers.GetDate(row, colDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var region = ExcelHelpers.GetString(row, colRegion) ?? "UNSPECIFIED";
            var resourceId = lookup.Resolve(
                ExcelHelpers.GetString(row, colPsid),
                ExcelHelpers.GetString(row, colEmail),
                ExcelHelpers.GetString(row, colResource));

            int? accountId = null;
            var accountName = ExcelHelpers.GetString(row, colAccount);
            if (!string.IsNullOrWhiteSpace(accountName))
            {
                var account = await resolver.ResolveAsync(accountName, region, ct);
                await _db.SaveChangesAsync(ct);
                accountId = account.AccountId;
            }

            _db.EngagementFacts.Add(new EngagementFact
            {
                Date = date,
                ResourceId = resourceId,
                AccountId = accountId,
                MeetingName = ExcelHelpers.GetString(row, colMeeting),
                Duration = ExcelHelpers.GetDouble(row, colDuration),
                Region = region,
                Remarks = ExcelHelpers.GetString(row, colRemarks)
            });
            inserted++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("EngagementImportService inserted {Count} engagement records", inserted);
        return inserted;
    }
}
