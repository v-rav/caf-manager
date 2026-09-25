using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using ClosedXML.Excel;

namespace CafPortal.Infrastructure.Export;

/// <summary>ClosedXML-based export of the main grids and an executive summary workbook.</summary>
public class ExportService(
    IResourceService resources,
    ICapacityService capacity,
    INominationService nominations,
    IPerformanceReviewService performance,
    IDashboardService dashboard) : IExportService
{
    private const string HeaderHtml = "#0F6CBD";

    public async Task<byte[]> ResourcesAsync(string? region, CancellationToken ct = default)
    {
        var rows = await resources.GetAllAsync(new ResourceQuery { Region = region }, ct);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Resources");
        var headers = new[] { "Name", "PSID", "Region", "Role", "Primary Skill", "Experience (yrs)", "Accounts", "Capacity Limit", "Utilization %", "Capacity Status", "Onboarding", "Active", "On Leave Today" };
        WriteHeader(ws, headers);
        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.Name;
            ws.Cell(r, 2).Value = x.Psid ?? "";
            ws.Cell(r, 3).Value = x.Region;
            ws.Cell(r, 4).Value = x.Role;
            ws.Cell(r, 5).Value = x.PrimarySkill ?? "";
            ws.Cell(r, 6).Value = x.ExperienceYears;
            ws.Cell(r, 7).Value = x.AccountCount;
            ws.Cell(r, 8).Value = x.CapacityLimit;
            ws.Cell(r, 9).Value = x.UtilizationPercent;
            ws.Cell(r, 10).Value = x.CapacityStatus;
            ws.Cell(r, 11).Value = x.OnboardingStatus;
            ws.Cell(r, 12).Value = x.ActiveFlag ? "Active" : "Inactive";
            ws.Cell(r, 13).Value = x.OnLeaveToday ? "Yes" : "No";
            r++;
        }
        return Finish(wb, ws, headers.Length);
    }

    public async Task<byte[]> CapacityAsync(string? region, CancellationToken ct = default)
    {
        var rows = await capacity.GetAsync(region, ct);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Capacity");
        var headers = new[] { "Resource", "Region", "Role", "Accounts", "Capacity Limit", "Utilization %", "Status", "Heat" };
        WriteHeader(ws, headers);
        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.ResourceName;
            ws.Cell(r, 2).Value = x.Region;
            ws.Cell(r, 3).Value = x.Role;
            ws.Cell(r, 4).Value = x.AccountCount;
            ws.Cell(r, 5).Value = x.CapacityLimit;
            ws.Cell(r, 6).Value = x.UtilizationPercent;
            ws.Cell(r, 7).Value = x.CapacityStatus;
            ws.Cell(r, 8).Value = x.HeatColor;
            r++;
        }
        return Finish(wb, ws, headers.Length);
    }

    public async Task<byte[]> NominationsAsync(string? region, CancellationToken ct = default)
    {
        var rows = await nominations.GetAsync(region, status: null, ct);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Nominations");
        var headers = new[] { "Account", "TPID", "Technology", "Region", "Migration Status", "Current State", "Status", "PM", "CFTL", "SA", "Blocker Reason", "Blocked Since", "Follow-up", "Age In Stage (days)", "Stale Tier", "Waves", "Wave Types", "Remarks" };
        WriteHeader(ws, headers);
        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.AccountName ?? "";
            ws.Cell(r, 2).Value = x.Tpid ?? "";
            ws.Cell(r, 3).Value = x.Technology ?? "";
            ws.Cell(r, 4).Value = x.Region;
            ws.Cell(r, 5).Value = x.MigrationStatus ?? "";
            ws.Cell(r, 6).Value = x.CurrentState ?? "";
            ws.Cell(r, 7).Value = x.Status;
            ws.Cell(r, 8).Value = x.ProjectCoordinator ?? "";
            ws.Cell(r, 9).Value = x.CftlPrimary ?? "";
            ws.Cell(r, 10).Value = x.SolutionArchitect ?? "";
            ws.Cell(r, 11).Value = x.BlockedReason ?? "";
            ws.Cell(r, 12).Value = x.BlockedSince?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(r, 13).Value = x.FollowUpDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(r, 14).Value = x.DaysSinceUpdate;
            ws.Cell(r, 15).Value = x.StaleTier;
            ws.Cell(r, 16).Value = string.Join(", ", x.Waves.Select(w => $"{w.WaveType}:{w.Reference}"));
            ws.Cell(r, 17).Value = x.WaveCount == 0 ? "None" : string.Join(", ", x.Waves.Select(w => w.WaveType).Distinct());
            ws.Cell(r, 18).Value = x.Remarks ?? "";
            r++;
        }
        return Finish(wb, ws, headers.Length);
    }

    public async Task<byte[]> PerformanceAsync(string? region, CancellationToken ct = default)
    {
        var rows = await performance.GetLatestAsync(region, ct);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Performance");
        var headers = new[] { "Name", "Role", "Reporting Manager", "Comm. Verbal", "Comm. Written", "Attitude", "Process", "Offering", "Score", "Pending", "Training Needs", "Review Date" };
        WriteHeader(ws, headers);
        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.PersonName;
            ws.Cell(r, 2).Value = x.Role ?? "";
            ws.Cell(r, 3).Value = x.ReportingManager ?? "";
            SetNullable(ws.Cell(r, 4), x.CommunicationVerbal);
            SetNullable(ws.Cell(r, 5), x.CommunicationWritten);
            SetNullable(ws.Cell(r, 6), x.Attitude);
            SetNullable(ws.Cell(r, 7), x.ProcessUnderstanding);
            SetNullable(ws.Cell(r, 8), x.OfferingUnderstanding);
            SetNullable(ws.Cell(r, 9), x.Score);
            ws.Cell(r, 10).Value = x.Pending ? "Yes" : "No";
            ws.Cell(r, 11).Value = string.Join(", ", x.TrainingNeeds);
            ws.Cell(r, 12).Value = x.ReviewDate.ToString("yyyy-MM-dd");
            r++;
        }
        return Finish(wb, ws, headers.Length);
    }

    public async Task<byte[]> ExecutiveSummaryAsync(string? region, CancellationToken ct = default)
    {
        var d = await dashboard.GetExecutiveAsync(region, ct);
        using var wb = new XLWorkbook();

        var ws = wb.AddWorksheet("Summary");
        ws.Cell(1, 1).Value = "CAF Operations Portal — Executive Summary";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Scope: {(string.IsNullOrEmpty(region) ? "Global" : region)}   Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        WriteHeader(ws, new[] { "Metric", "Value" }, startRow: 4);
        var kpis = new (string, int)[]
        {
            ("Total Resources", d.TotalResources),
            ("Active Accounts", d.ActiveAccounts),
            ("Available", d.AvailableResources),
            ("Partially Utilized", d.PartiallyUtilizedResources),
            ("Fully Utilized", d.FullyUtilizedResources),
            ("Overloaded", d.OverloadedResources),
            ("On Leave", d.ResourcesOnLeave),
            ("Strategic Accounts", d.StrategicAccounts),
            ("Open Nominations", d.OpenNominations),
        };
        var r = 5;
        foreach (var (label, value) in kpis)
        {
            ws.Cell(r, 1).Value = label;
            ws.Cell(r, 2).Value = value;
            r++;
        }
        ws.Columns(1, 2).AdjustToContents();

        WriteDistribution(wb, "Region Distribution", d.RegionDistribution);
        WriteDistribution(wb, "Capacity Distribution", d.CapacityDistribution);
        WriteDistribution(wb, "Strategic Coverage", d.StrategicAccountCoverage);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteDistribution(XLWorkbook wb, string name, IReadOnlyList<NameValueDto> data)
    {
        var ws = wb.AddWorksheet(name.Length > 31 ? name[..31] : name);
        WriteHeader(ws, new[] { "Name", "Value" });
        var r = 2;
        foreach (var item in data)
        {
            ws.Cell(r, 1).Value = item.Name;
            ws.Cell(r, 2).Value = item.Value;
            r++;
        }
        ws.Columns(1, 2).AdjustToContents();
    }

    private static void SetNullable(IXLCell cell, double? value)
    {
        if (value.HasValue) cell.Value = value.Value;
        else cell.Value = "";
    }

    private static void WriteHeader(IXLWorksheet ws, string[] headers, int startRow = 1)
    {
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(startRow, i + 1).Value = headers[i];
        var head = ws.Range(startRow, 1, startRow, headers.Length);
        head.Style.Font.Bold = true;
        head.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderHtml);
        head.Style.Font.FontColor = XLColor.White;
    }

    private static byte[] Finish(XLWorkbook wb, IXLWorksheet ws, int cols)
    {
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, cols).AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
