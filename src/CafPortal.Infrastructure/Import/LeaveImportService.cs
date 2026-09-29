using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using CafPortal.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CafPortal.Infrastructure.Import;

/// <summary>
/// Loads "LeaveCal.xlsx". Supports both a long layout (Resource | Date | LeaveType)
/// and a calendar/matrix layout (Resource in first columns, one column per date).
/// </summary>
public class LeaveImportService(AppDbContext db, ILogger<LeaveImportService> logger) : ILeaveImportService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<LeaveImportService> _logger = logger;

    public async Task<int> ImportAsync(Stream workbook, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(workbook);
        var ws = wb.Worksheets.First();
        var rows = ws.RowsUsed().ToList();
        if (rows.Count < 2)
            return 0;

        var lookup = await ResourceLookup.BuildAsync(_db, ct);
        var headers = ExcelHelpers.MapHeaders(rows[0]);

        // Portal is the system of record for leave: append only, skipping (resource, date) pairs that
        // already exist so a re-import never duplicates or wipes portal-entered leave.
        var seen = (await _db.LeaveFacts.AsNoTracking()
                .Select(l => new { l.ResourceId, l.LeaveDate }).ToListAsync(ct))
            .Select(x => (x.ResourceId, x.LeaveDate)).ToHashSet();

        var colDate = ExcelHelpers.FindColumn(headers, "LeaveDate", "Date");
        var colType = ExcelHelpers.FindColumn(headers, "LeaveType", "Type", "Leave");
        var isLongFormat = colDate is not null && colType is not null;

        var inserted = isLongFormat
            ? await ImportLongFormatAsync(rows, headers, lookup, colDate!.Value, colType!.Value, seen, ct)
            : ImportCalendarFormat(rows, headers, lookup, seen);

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("LeaveImportService inserted {Count} leave records ({Mode})",
            inserted, isLongFormat ? "long" : "calendar");
        return inserted;
    }

    private Task<int> ImportLongFormatAsync(List<IXLRow> rows, Dictionary<string, int> headers,
        ResourceLookup lookup, int colDate, int colType, HashSet<(int, DateOnly)> seen, CancellationToken ct)
    {
        var colPsid = ExcelHelpers.FindColumn(headers, "PSID", "PS ID");
        var colEmail = ExcelHelpers.FindColumn(headers, "Email");
        var colName = ExcelHelpers.FindColumn(headers, "Resource", "Name");
        var inserted = 0;

        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;
            var resourceId = lookup.Resolve(
                ExcelHelpers.GetString(row, colPsid),
                ExcelHelpers.GetString(row, colEmail),
                ExcelHelpers.GetString(row, colName));
            var date = ExcelHelpers.GetDate(row, colDate);
            var type = ExcelHelpers.GetString(row, colType) ?? "Leave";
            if (resourceId is null || date is null)
                continue;
            if (!seen.Add((resourceId.Value, date.Value)))
                continue;

            _db.LeaveFacts.Add(new LeaveFact
            {
                ResourceId = resourceId.Value,
                LeaveDate = date.Value,
                LeaveType = type
            });
            inserted++;
        }
        return Task.FromResult(inserted);
    }

    private int ImportCalendarFormat(List<IXLRow> rows, Dictionary<string, int> headers, ResourceLookup lookup, HashSet<(int, DateOnly)> seen)
    {
        var colPsid = ExcelHelpers.FindColumn(headers, "PSID", "PS ID");
        var colEmail = ExcelHelpers.FindColumn(headers, "Email");
        var colName = ExcelHelpers.FindColumn(headers, "Resource", "Name");

        // Date columns are those whose header parses to a date.
        var dateColumns = new List<(int Column, DateOnly Date)>();
        foreach (var kvp in headers)
        {
            if (DateTime.TryParse(kvp.Key, out var dt))
                dateColumns.Add((kvp.Value, DateOnly.FromDateTime(dt)));
        }
        if (dateColumns.Count == 0)
            return 0;

        var inserted = 0;
        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;
            var resourceId = lookup.Resolve(
                ExcelHelpers.GetString(row, colPsid),
                ExcelHelpers.GetString(row, colEmail),
                ExcelHelpers.GetString(row, colName));
            if (resourceId is null)
                continue;

            foreach (var (column, date) in dateColumns)
            {
                var marker = ExcelHelpers.GetString(row, column);
                if (string.IsNullOrWhiteSpace(marker))
                    continue;
                if (!seen.Add((resourceId.Value, date)))
                    continue;
                _db.LeaveFacts.Add(new LeaveFact
                {
                    ResourceId = resourceId.Value,
                    LeaveDate = date,
                    LeaveType = marker
                });
                inserted++;
            }
        }
        return inserted;
    }
}
