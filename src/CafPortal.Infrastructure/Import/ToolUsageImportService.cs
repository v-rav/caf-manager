using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities.Governance;
using CafPortal.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CafPortal.Infrastructure.Import;

/// <summary>Loads the FDO "EMEA Nomination Wize Tool Usage" export. One nomination (TPID + Task Id) can
/// have many usage rows (1-to-many). Captures Account ID, TPID, Task Id, Details, Category, Tool Name,
/// Created Date and resolves the internal nomination by Task Id. Portal-owned fact — wipe-and-reload.</summary>
public class ToolUsageImportService(AppDbContext db, ILogger<ToolUsageImportService> logger) : IToolUsageImportService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<ToolUsageImportService> _logger = logger;

    public async Task<int> ImportAsync(Stream workbook, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(workbook);
        var ws = wb.Worksheets.First(); // the data sheet (the second "GHCPUsage" sheet is just a pivot summary)
        var rows = ws.RowsUsed().ToList();
        if (rows.Count < 2)
            return 0;

        var headers = ExcelHelpers.MapHeaders(rows[0]);
        const string K = CafPortal.Application.Common.ImportProfiles.ToolUsage;
        var colAccountId = ExcelHelpers.Col(headers, K, "AccountId");
        var colTpid = ExcelHelpers.Col(headers, K, "Tpid");
        var colTaskId = ExcelHelpers.Col(headers, K, "TaskId");
        var colDetails = ExcelHelpers.Col(headers, K, "Details");
        var colCategory = ExcelHelpers.Col(headers, K, "Category");
        var colTool = ExcelHelpers.Col(headers, K, "ToolName");
        var colCreated = ExcelHelpers.Col(headers, K, "CreatedDate");

        // Task Id -> internal nomination id, for 1-to-many linkage.
        var taskToNom = await _db.Nominations
            .Where(n => n.ExternalTaskId != null)
            .Select(n => new { n.ExternalTaskId, n.Id })
            .ToDictionaryAsync(x => x.ExternalTaskId!, x => x.Id, StringComparer.OrdinalIgnoreCase, ct);

        // Wipe-and-reload: the drop is the authoritative current list (portal never edits these rows).
        await _db.NominationToolUsageFacts.ExecuteDeleteAsync(ct);

        var inserted = 0;
        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;

            var taskId = ExcelHelpers.GetString(row, colTaskId);
            var tool = ExcelHelpers.GetString(row, colTool);
            var details = ExcelHelpers.GetString(row, colDetails);
            // Skip rows with no identifying content (blank tail rows / stray cells).
            if (taskId is null && tool is null && details is null)
                continue;

            int? nominationId = taskId is not null && taskToNom.TryGetValue(taskId, out var id) ? id : null;

            _db.NominationToolUsageFacts.Add(new NominationToolUsageFact
            {
                ExternalAccountId = ExcelHelpers.GetString(row, colAccountId),
                Tpid = ExcelHelpers.GetString(row, colTpid),
                TaskId = taskId,
                NominationId = nominationId,
                Details = details,
                Category = ExcelHelpers.GetString(row, colCategory),
                ToolName = tool,
                CreatedDate = ExcelHelpers.GetDate(row, colCreated),
            });
            inserted++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Tool-usage import: {Count} rows loaded", inserted);
        return inserted;
    }
}
