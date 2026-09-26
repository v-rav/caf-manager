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
    IReconciliationService reconciliation,
    IAnalyticsService analytics,
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
        var headers = new[] { "Resource", "Region", "Role", "Accounts", "Assigned Accounts", "Capacity Limit", "Utilization %", "Status", "Heat" };
        WriteHeader(ws, headers);
        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.ResourceName;
            ws.Cell(r, 2).Value = x.Region;
            ws.Cell(r, 3).Value = x.Role;
            ws.Cell(r, 4).Value = x.AccountCount;
            ws.Cell(r, 5).Value = string.Join("; ", x.Accounts);
            ws.Cell(r, 6).Value = x.CapacityLimit;
            ws.Cell(r, 7).Value = x.UtilizationPercent;
            ws.Cell(r, 8).Value = x.CapacityStatus;
            ws.Cell(r, 9).Value = x.HeatColor;
            r++;
        }
        return Finish(wb, ws, headers.Length);
    }

    public async Task<byte[]> NominationsAsync(string? region, string? approval = null, string? migrationStatus = null,
        string? currentState = null, string? sla = null, string? links = null, string? search = null, CancellationToken ct = default)
    {
        var all = await nominations.GetAsync(region, status: null, ct);
        // Apply the same filters the Nominations grid uses, so the export matches the on-screen view.
        var rows = all.Where(x =>
            (string.IsNullOrEmpty(approval) || string.Equals(x.ApprovalStatus ?? "", approval, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(migrationStatus) || string.Equals(x.MigrationStatus, migrationStatus, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(currentState) || string.Equals(x.CurrentState, currentState, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(sla) || string.Equals(x.StaleTier, sla, StringComparison.OrdinalIgnoreCase)) &&
            MatchLinks(x, links) &&
            (string.IsNullOrEmpty(search) || (x.AccountName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Nominations");
        var headers = new[] { "Account", "TPID", "Technology", "Region", "Stage", "Migration Status", "Current State", "Status", "Approval", "PM", "CFTL", "SA", "Blocker Reason", "Blocked Since", "Follow-up", "Age In Stage (days)", "Stale Tier", "Waves", "Wave Types", "Remarks" };
        WriteHeader(ws, headers);
        var r = 2;
        foreach (var x in rows)
        {
            var c = 1;
            ws.Cell(r, c++).Value = x.AccountName ?? "";
            ws.Cell(r, c++).Value = x.Tpid ?? "";
            ws.Cell(r, c++).Value = x.Technology ?? "";
            ws.Cell(r, c++).Value = x.Region;
            var stage = StageNumber(x.MigrationStatus);
            if (stage is int sn) ws.Cell(r, c++).Value = sn; else ws.Cell(r, c++).Value = "";
            ws.Cell(r, c++).Value = x.MigrationStatus ?? "";
            ws.Cell(r, c++).Value = x.CurrentState ?? "";
            ws.Cell(r, c++).Value = x.Status;
            ws.Cell(r, c++).Value = x.ApprovalStatus ?? "";
            ws.Cell(r, c++).Value = x.ProjectCoordinator ?? "";
            ws.Cell(r, c++).Value = x.CftlPrimary ?? "";
            ws.Cell(r, c++).Value = x.SolutionArchitect ?? "";
            ws.Cell(r, c++).Value = x.BlockedReason ?? "";
            ws.Cell(r, c++).Value = x.BlockedSince?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(r, c++).Value = x.FollowUpDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(r, c++).Value = x.DaysSinceUpdate;
            ws.Cell(r, c++).Value = x.StaleTier;
            ws.Cell(r, c++).Value = string.Join(", ", x.Waves.Select(w => $"{w.WaveType}:{w.Reference}"));
            ws.Cell(r, c++).Value = x.WaveCount == 0 ? "None" : string.Join(", ", x.Waves.Select(w => w.WaveType).Distinct());
            ws.Cell(r, c++).Value = x.Remarks ?? "";
            r++;
        }
        ws.Range(1, 1, rows.Count + 1, headers.Length).SetAutoFilter();

        BuildNominationAnalysis(wb, rows, region, approval);
        return Finish(wb, ws, headers.Length);
    }

    // Migration Status text → 1–4 stage number (matches the grid), or null when no stage is recognised.
    private static int? StageNumber(string? migration)
    {
        var t = (migration ?? "").ToLowerInvariant();
        if (t.Contains("validating")) return 1;
        if (t.Contains("pre-requisite")) return 2;
        if (t.Contains("finalize")) return 3;
        if (t.Contains("executing migration")) return 4;
        return null;
    }

    private static bool MatchLinks(NominationDto x, string? links) => links switch
    {
        null or "" => true,
        "No waves" => x.NoWavesLinked,
        "Has any waves" => !x.NoWavesLinked,
        "Has DB" => x.DbLinked,
        "Has Security" => x.SecurityLinked,
        _ => true,
    };

    // Second sheet: pivot-style counts of the filtered rows so a lead gets a one-glance breakdown.
    private void BuildNominationAnalysis(XLWorkbook wb, IReadOnlyList<NominationDto> rows, string? region, string? approval)
    {
        var ws = wb.AddWorksheet("Analysis");
        var r = 1;
        ws.Cell(r, 1).Value = "Nomination Analysis";
        ws.Cell(r, 1).Style.Font.Bold = true;
        ws.Cell(r, 1).Style.Font.FontSize = 14;
        r += 2;
        ws.Cell(r++, 1).Value = $"Scope: {(string.IsNullOrEmpty(region) ? "All regions" : region)} \u00b7 {(string.IsNullOrEmpty(approval) ? "All approvals" : approval)}";
        ws.Cell(r++, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        ws.Cell(r++, 1).Value = $"Total nominations in scope: {rows.Count}";
        r++;

        void Section(string title, IEnumerable<(string Label, int Count)> items)
        {
            ws.Cell(r, 1).Value = title;
            var head = ws.Range(r, 1, r, 2);
            head.Style.Font.Bold = true;
            head.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderHtml);
            head.Style.Font.FontColor = XLColor.White;
            r++;
            foreach (var it in items) { ws.Cell(r, 1).Value = it.Label; ws.Cell(r, 2).Value = it.Count; r++; }
            r++;
        }

        Section("By approval status", rows
            .GroupBy(x => string.IsNullOrEmpty(x.ApprovalStatus) ? "(blank)" : x.ApprovalStatus!)
            .OrderByDescending(g => g.Count()).Select(g => (g.Key, g.Count())));

        var stageItems = new[] { 1, 2, 3, 4 }
            .Select(n => ($"Stage {n}", rows.Count(x => StageNumber(x.MigrationStatus) == n))).ToList();
        stageItems.Add(("No stage", rows.Count(x => StageNumber(x.MigrationStatus) == null)));
        Section("By migration stage", stageItems);

        Section("By region", rows
            .GroupBy(x => string.IsNullOrEmpty(x.Region) ? "(blank)" : x.Region)
            .OrderByDescending(g => g.Count()).Select(g => (g.Key, g.Count())));

        var slaItems = new List<(string, int)> { ("On track", rows.Count(x => string.IsNullOrEmpty(x.StaleTier))) };
        slaItems.AddRange(new[] { "Warn", "Escalate", "Defer" }.Select(t => (t, rows.Count(x => x.StaleTier == t))));
        Section("By SLA stale tier", slaItems);

        Section("Wave linkage", new[]
        {
            ("DB linked", rows.Count(x => x.DbLinked)),
            ("Security linked", rows.Count(x => x.SecurityLinked)),
            ("No waves linked", rows.Count(x => x.NoWavesLinked)),
        });

        ws.Columns(1, 2).AdjustToContents();
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

    // One workbook capturing the whole Analytics page: KPIs, distributions, value cuts, and the current Trend view.
    public async Task<byte[]> AnalyticsAsync(string? region, string basis, string granularity, string measure,
        string? splitBy, int? fy, CancellationToken ct = default)
    {
        var a = await analytics.GetAsync(region, ct);
        var ts = await analytics.GetTimeSeriesAsync(region, basis, granularity, measure, splitBy, null, null, fy, ct);

        using var wb = new XLWorkbook();

        var ws = wb.AddWorksheet("Overview");
        ws.Cell(1, 1).Value = "CAF Operations Portal \u2014 Migration Analytics";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Scope: {(string.IsNullOrEmpty(region) ? "Global" : region)}   Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
        WriteHeader(ws, new[] { "Metric", "Value" }, startRow: 4);
        var kpis = new (string Label, double Value)[]
        {
            ("Total Approved", a.TotalApproved), ("In-Flight", a.InFlight), ("Completed", a.Completed),
            ("Total ACR", a.TotalAcr), ("NNR ACR", a.NnrAcr), ("Total Cores", a.TotalCores), ("With ACR data", a.WithAcr),
            ("Tool Attached", a.ToolAttached), ("Automation Used", a.AutomationUsed), ("Tool/Automation flag denom", a.ToolFlagDenom),
        };
        var kr = 5;
        foreach (var (label, value) in kpis) { ws.Cell(kr, 1).Value = label; ws.Cell(kr, 2).Value = value; kr++; }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 2).AdjustToContents();

        WriteStackedSections(wb, "Distributions",
            ("By stage", a.ByStage), ("Current-state health", a.ByHealth), ("SLA stale tier", a.BySla),
            ("By region", a.ByRegion), ("By segment", a.BySegment), ("By migration path", a.ByMigrationPath),
            ("By mode of access", a.ByModeOfAccess), ("Wave linkage", a.WaveLinkage));

        WriteStackedSections(wb, "Value cuts",
            ("ACR by region", a.AcrByRegion), ("ACR by segment", a.AcrBySegment), ("ACR by migration path", a.AcrByMigrationPath),
            ("Cores by stage", a.CoresByStage), ("Top partners by ACR", a.TopPartnersByAcr));

        BuildTrendSheet(wb, ts);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // Several label/value distributions stacked on one sheet (fewer tabs than one sheet each).
    private static void WriteStackedSections(XLWorkbook wb, string sheetName, params (string Title, IReadOnlyList<NameValueDto> Data)[] sections)
    {
        var ws = wb.AddWorksheet(sheetName.Length > 31 ? sheetName[..31] : sheetName);
        var r = 1;
        foreach (var (title, data) in sections)
        {
            ws.Cell(r, 1).Value = title;
            var head = ws.Range(r, 1, r, 2);
            head.Style.Font.Bold = true;
            head.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderHtml);
            head.Style.Font.FontColor = XLColor.White;
            r++;
            foreach (var it in data) { ws.Cell(r, 1).Value = it.Name; ws.Cell(r, 2).Value = it.Value; r++; }
            r++;
        }
        ws.Columns(1, 2).AdjustToContents();
    }

    // The current Trend view as a pivot: Period x series x Total (matches the on-screen table).
    private static void BuildTrendSheet(XLWorkbook wb, TimeSeriesDto ts)
    {
        var ws = wb.AddWorksheet("Trend");
        ws.Cell(1, 1).Value = $"Trend \u2014 {ts.Basis} \u00b7 {ts.Granularity} \u00b7 {ts.Measure}{(ts.SplitBy != "none" ? $" \u00b7 split by {ts.SplitBy}" : "")}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(2, 1).Value = $"Buckets: {ts.Buckets.Count} \u00b7 Total: {ts.Total} \u00b7 Records without a {ts.Basis} date: {ts.RecordsWithoutDate}";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        var multi = ts.Series.Count > 1;
        var headers = multi
            ? new[] { "Period" }.Concat(ts.Series).Concat(new[] { "Total" }).ToArray()
            : new[] { "Period", ts.Measure };
        WriteHeader(ws, headers, startRow: 4);
        var r = 5;
        foreach (var b in ts.Buckets)
        {
            ws.Cell(r, 1).Value = b.Label;
            if (multi)
            {
                for (int i = 0; i < ts.Series.Count; i++)
                    ws.Cell(r, i + 2).Value = b.Values.FirstOrDefault(v => v.Name == ts.Series[i])?.Value ?? 0;
                ws.Cell(r, ts.Series.Count + 2).Value = b.Total;
            }
            else ws.Cell(r, 2).Value = b.Total;
            r++;
        }
        if (ts.Buckets.Count > 0) ws.Range(4, 1, ts.Buckets.Count + 4, headers.Length).SetAutoFilter();
        ws.SheetView.FreezeRows(4);
        ws.Columns(1, headers.Length).AdjustToContents();
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

    public async Task<byte[]> ReconciliationAsync(string? region, CancellationToken ct = default)
    {
        var report = await reconciliation.GetAsync(region, ct);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Reconciliation");
        var headers = new[] { "Resource", "Region", "Account (linked)", "TPID", "Segment", "Relationship", "In Master", "In-Flight", "Match", "Suggested Account", "Suggested TPID", "Utilization" };
        WriteHeader(ws, headers);
        var r = 2;
        foreach (var x in report.Rows)
        {
            ws.Cell(r, 1).Value = x.ResourceName;
            ws.Cell(r, 2).Value = x.ResourceRegion;
            ws.Cell(r, 3).Value = x.AccountName;
            ws.Cell(r, 4).Value = x.Tpid ?? "";
            ws.Cell(r, 5).Value = x.Segment ?? "";
            ws.Cell(r, 6).Value = x.RelationshipType ?? "";
            ws.Cell(r, 7).Value = x.InMaster ? "Yes" : "No";
            ws.Cell(r, 8).Value = x.InFlight ? "Yes" : "No";
            ws.Cell(r, 9).Value = x.MatchState;
            ws.Cell(r, 10).Value = x.SuggestedAccountName ?? "";
            ws.Cell(r, 11).Value = x.SuggestedTpid ?? "";
            ws.Cell(r, 12).Value = x.UtilizationEffect;
            r++;
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, headers.Length).AdjustToContents();

        var s = report.Summary;
        var sum = wb.AddWorksheet("Summary");
        WriteHeader(sum, new[] { "Metric", "Value" });
        var metrics = new (string Label, int Value)[]
        {
            ("Total links", s.TotalLinks), ("Resources", s.Resources), ("Linked accounts", s.LinkedAccounts),
            ("Master (has TPID)", s.Master), ("Suggest merge", s.SuggestMerge), ("Orphan", s.Orphan),
            ("Keep (in-flight)", s.Keep), ("Drop (not in-flight)", s.Drop), ("In-flight accounts", s.InFlightAccounts)
        };
        var sr = 2;
        foreach (var (label, value) in metrics)
        {
            sum.Cell(sr, 1).Value = label;
            sum.Cell(sr, 2).Value = value;
            sr++;
        }
        sum.SheetView.FreezeRows(1);
        sum.Columns(1, 2).AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
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
