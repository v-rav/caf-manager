using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>
/// Migration analytics over Approved nominations (region-scoped). Reuses INominationService so it inherits
/// the same stage/stale/wave derivations and the Offerings-enriched fields; joins Segment by AccountId.
/// </summary>
public class AnalyticsService(INominationService nominations, IApplicationDbContext db, IAcrService acr) : IAnalyticsService
{
    private readonly INominationService _nominations = nominations;
    private readonly IApplicationDbContext _db = db;
    private readonly IAcrService _acr = acr;

    private const int DefaultContainerCoreFloor = 20;

    private static bool IsContainerPath(string? p)
    {
        var s = (p ?? string.Empty).ToLowerInvariant();
        return s.Contains("container") || s.Contains("aks") || s.Contains("aca") || s.Contains("eks") || s.Contains("ecs");
    }

    private static readonly string[] StageLabels =
        { "1 · Validating", "2 · Pre-Requisites", "3 · Finalize Scope", "4 · Executing Migration" };

    private static int? StageIndex(string? migration)
    {
        var m = migration?.ToLowerInvariant() ?? string.Empty;
        if (m.Contains("validating")) return 1;
        if (m.Contains("pre-requisite") || m.Contains("pre requisite") || m.Contains("prerequisite")) return 2;
        if (m.Contains("finalize")) return 3;
        if (m.Contains("executing migration")) return 4;
        return null;
    }

    private static string Health(string? currentState)
    {
        var t = (currentState ?? string.Empty).ToLowerInvariant();
        if (t.Contains("block")) return "Blocked";
        if (t.Contains("waiting") || t.Contains("follow")) return "Waiting";
        if (t.Contains("on track")) return "On Track";
        return "Other";
    }

    public async Task<AnalyticsDto> GetAsync(string? region, CancellationToken ct = default)
    {
        var all = await _nominations.GetAsync(region, status: null, ct);
        var noms = all.Where(n => string.Equals(n.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)).ToList();

        // Segment isn't on NominationDto — look it up by AccountId.
        var accountIds = noms.Where(n => n.AccountId != null).Select(n => n.AccountId!.Value).Distinct().ToList();
        var segByAccount = await _db.Accounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.AccountId))
            .Select(a => new { a.AccountId, a.Segment })
            .ToDictionaryAsync(a => a.AccountId, a => a.Segment, ct);
        string Seg(int? id) => id != null && segByAccount.TryGetValue(id.Value, out var s) && !string.IsNullOrWhiteSpace(s) ? s! : "Unspecified";
        string Reg(string? r) => string.IsNullOrWhiteSpace(r) ? "Unspecified" : r!;

        static bool IsSettled(NominationDto n)
        {
            var s = (n.Status ?? string.Empty).ToLowerInvariant();
            return s.Contains("complete") || s.Contains("closed") || s.Contains("withdraw") || s.Contains("defer");
        }

        List<NameValueDto> Count(IEnumerable<IGrouping<string, NominationDto>> groups) =>
            groups.Select(g => new NameValueDto(g.Key, g.Count())).OrderByDescending(x => x.Value).ToList();

        var dto = new AnalyticsDto
        {
            TotalApproved = noms.Count,
            InFlight = noms.Count(n => !IsSettled(n)),
            Completed = noms.Count(n => { var s = (n.Status ?? string.Empty).ToLowerInvariant(); return s.Contains("complete") || s.Contains("closed"); }),
            TotalAcr = (double)noms.Sum(n => n.TotalAcr ?? 0m),
            NnrAcr = (double)noms.Sum(n => n.NnrAcr ?? 0m),
            TotalCores = noms.Sum(n => n.TotalCores ?? 0),
            WithAcr = noms.Count(n => n.TotalAcr != null),
            ToolAttached = noms.Count(n => n.IsToolAttached == true),
            AutomationUsed = noms.Count(n => n.IsAutomationUsed == true),
            ToolFlagDenom = noms.Count(n => n.IsToolAttached != null || n.IsAutomationUsed != null),
            WithKickoffLag = noms.Count(n => n.KickoffToStartLagDays != null),
            AvgKickoffToStartLagDays = noms.Any(n => n.KickoffToStartLagDays != null)
                ? Math.Round(noms.Where(n => n.KickoffToStartLagDays != null).Average(n => n.KickoffToStartLagDays!.Value), 1)
                : 0,

            ByStage = Enumerable.Range(1, 4)
                .Select(s => new NameValueDto(StageLabels[s - 1], noms.Count(n => StageIndex(n.MigrationStatus) == s)))
                .ToList(),
            ByHealth = Count(noms.GroupBy(n => Health(n.CurrentState))),
            BySla = new[] { "Warn", "Escalate", "Defer" }
                .Select(t => new NameValueDto(t, noms.Count(n => n.StaleTier == t))).ToList(),
            ByRegion = Count(noms.GroupBy(n => Reg(n.Region))),
            BySegment = Count(noms.GroupBy(n => Seg(n.AccountId))),
            ByMigrationPath = Count(noms.Where(n => !string.IsNullOrWhiteSpace(n.PrimaryMigrationPath))
                .GroupBy(n => n.PrimaryMigrationPath!)).Take(8).ToList(),
            ByModeOfAccess = Count(noms.Where(n => !string.IsNullOrWhiteSpace(n.ModeOfAccess))
                .GroupBy(n => n.ModeOfAccess!)),
            WaveLinkage = new List<NameValueDto>
            {
                new("DB", noms.Count(n => n.DbLinked)),
                new("Security", noms.Count(n => n.SecurityLinked)),
                new("Landing Zone", noms.Count(n => n.AlzLinked)),
                new("No waves", noms.Count(n => n.NoWavesLinked)),
            },

            AcrByRegion = noms.GroupBy(n => Reg(n.Region))
                .Select(g => new NameValueDto(g.Key, (double)g.Sum(n => n.TotalAcr ?? 0m)))
                .Where(x => x.Value > 0).OrderByDescending(x => x.Value).ToList(),
            AcrBySegment = noms.GroupBy(n => Seg(n.AccountId))
                .Select(g => new NameValueDto(g.Key, (double)g.Sum(n => n.TotalAcr ?? 0m)))
                .Where(x => x.Value > 0).OrderByDescending(x => x.Value).ToList(),
            AcrByMigrationPath = noms.Where(n => !string.IsNullOrWhiteSpace(n.PrimaryMigrationPath))
                .GroupBy(n => n.PrimaryMigrationPath!)
                .Select(g => new NameValueDto(g.Key, (double)g.Sum(n => n.TotalAcr ?? 0m)))
                .Where(x => x.Value > 0).OrderByDescending(x => x.Value).Take(8).ToList(),
            CoresByStage = Enumerable.Range(1, 4)
                .Select(s => new NameValueDto(StageLabels[s - 1], (double)noms.Where(n => StageIndex(n.MigrationStatus) == s).Sum(n => n.TotalCores ?? 0)))
                .ToList(),
            TopPartnersByAcr = noms.Where(n => !string.IsNullOrWhiteSpace(n.PartnerName))
                .GroupBy(n => n.PartnerName!)
                .Select(g => new NameValueDto(g.Key, (double)g.Sum(n => n.TotalAcr ?? 0m)))
                .Where(x => x.Value > 0).OrderByDescending(x => x.Value).Take(8).ToList(),
            // Spans ALL approval steps (not just Approved) — mirrors the FDO "Concierge Nomination Status ACR".
            AcrByApproval = all.Where(n => !string.IsNullOrWhiteSpace(n.ApprovalStatus))
                .GroupBy(n => n.ApprovalStatus!)
                .Select(g => new NameValueDto(g.Key, (double)g.Sum(n => n.TotalAcr ?? 0m)))
                .Where(x => x.Value > 0).OrderByDescending(x => x.Value).ToList(),
        };
        return dto;
    }

    public async Task<AcrCaptureDto> GetAcrCaptureAsync(string? region, CancellationToken ct = default)
    {
        var all = await _nominations.GetAsync(region, status: null, ct);
        var noms = all.Where(n => string.Equals(n.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)).ToList();
        var container = noms.Where(n => IsContainerPath(n.PrimaryMigrationPath)).ToList();

        var rates = await _acr.GetRatesAsync(ct);
        var months = rates.AnnualizationMonths <= 0 ? 12 : rates.AnnualizationMonths;

        // Core floor a containerized workload is assumed not to fall below (from the editable rate master).
        var floor = rates.ContainerCoreFloor > 0 ? (int)rates.ContainerCoreFloor : DefaultContainerCoreFloor;

        double RatePerCoreYear(string? path)
        {
            var win = (path ?? string.Empty).ToLowerInvariant().Contains("windows");
            return (double)((win ? rates.AksWindowsArpuPerCoreMonth : rates.AksLinuxArpuPerCoreMonth) * months);
        }

        var rows = new List<AcrCaptureRow>();
        foreach (var n in container)
        {
            var cores = n.TotalCores ?? 0;
            var acr = (double)(n.TotalAcr ?? 0m);
            string? gap = cores <= 0 ? "Missing cores"
                : (n.TotalAcr is null || n.TotalAcr == 0m) ? "Missing ACR"
                : cores <= 16 ? "Low cores"
                : null;
            if (gap is null) continue;

            var ratePerCoreYr = RatePerCoreYear(n.PrimaryMigrationPath);
            var estCores = gap == "Missing ACR" ? cores : Math.Max(cores, floor);
            var estAcr = estCores * ratePerCoreYr;
            var gapAcr = Math.Max(0, estAcr - acr);
            var stageIdx = StageIndex(n.MigrationStatus);
            var stageLabel = stageIdx is int si ? StageLabels[si - 1] : "—";
            var waveTypes = new List<string>();
            if (n.SecurityLinked) waveTypes.Add("Security");
            if (n.DbLinked) waveTypes.Add("DB");
            if (n.AlzLinked) waveTypes.Add("ALZ");
            var wavesStr = waveTypes.Count > 0 ? string.Join(" · ", waveTypes) : "None";
            rows.Add(new AcrCaptureRow(n.Id, n.AccountName ?? "—", n.Tpid, n.Region, n.PrimaryMigrationPath ?? "—",
                cores, Math.Round(acr), gap, Math.Round(ratePerCoreYr), estCores, Math.Round(estAcr), Math.Round(gapAcr),
                stageLabel, n.Status, n.CurrentState, n.SolutionArchitect, n.ProjectCoordinator, wavesStr));
        }
        rows = rows.OrderByDescending(r => r.GapAcr).ToList();

        var containerAcr = container.Sum(n => (double)(n.TotalAcr ?? 0m));
        var totalAcr = noms.Sum(n => (double)(n.TotalAcr ?? 0m));

        return new AcrCaptureDto
        {
            ContainerNoms = container.Count,
            ContainerAcr = Math.Round(containerAcr),
            ContainerAcrShare = totalAcr > 0 ? Math.Round(containerAcr / totalAcr * 100, 1) : 0,
            FlaggedCount = rows.Count,
            MissingCoresCount = rows.Count(r => r.GapType == "Missing cores"),
            MissingAcrCount = rows.Count(r => r.GapType == "Missing ACR"),
            LowCoresCount = rows.Count(r => r.GapType == "Low cores"),
            AcrAtRisk = Math.Round(container.Where(n => (n.TotalCores ?? 0) <= 0).Sum(n => (double)(n.TotalAcr ?? 0m))),
            EstimatedUpside = Math.Round(rows.Sum(r => r.GapAcr)),
            CoreFloor = floor,
            Rows = rows,
        };
    }

    public async Task<TimeSeriesDto> GetTimeSeriesAsync(string? region, string basis, string granularity, string measure,
        string? splitBy, DateOnly? from, DateOnly? to, int? fy, CancellationToken ct = default)
    {
        basis = (basis ?? "completed").ToLowerInvariant();
        granularity = (granularity ?? "month").ToLowerInvariant();
        measure = (measure ?? "count").ToLowerInvariant();
        var split = (splitBy ?? "none").ToLowerInvariant();
        var (noms, segByAccount) = await LoadApprovedAsync(region, ct);

        double Val(NominationDto n) => measure switch
        {
            "acr" => (double)(n.TotalAcr ?? 0m),
            "nnr" => (double)(n.NnrAcr ?? 0m),
            "cores" => n.TotalCores ?? 0,
            _ => 1,
        };
        string Ser(NominationDto n) => SeriesOf(n, split, segByAccount);

        var acc = new Dictionary<string, Dictionary<string, double>>();
        var meta = new Dictionary<string, (long Sort, string Label)>();
        var seriesSet = new HashSet<string>();
        var fySet = new HashSet<int>();
        int noDate = 0;
        foreach (var n in noms)
        {
            var d = BasisDate(n, basis);
            if (d is null) { noDate++; continue; }
            fySet.Add(FiscalCalendar.FiscalYear(d.Value));
            if (fy is not null && FiscalCalendar.FiscalYear(d.Value) != fy) continue;
            if (from is not null && d < from) continue;
            if (to is not null && d > to) continue;
            var (sort, key, label) = FiscalCalendar.Bucket(d.Value, granularity);
            meta[key] = (sort, label);
            var ser = Ser(n);
            seriesSet.Add(ser);
            if (!acc.TryGetValue(key, out var m)) { m = new(); acc[key] = m; }
            m[ser] = m.GetValueOrDefault(ser) + Val(n);
        }

        var series = seriesSet.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        var buckets = acc.Keys.OrderBy(k => meta[k].Sort)
            .Select(k => new TimeBucketDto
            {
                Key = k,
                Label = meta[k].Label,
                Values = series.Select(s => new NameValueDto(s, acc[k].GetValueOrDefault(s))).ToList(),
                Total = acc[k].Values.Sum(),
            }).ToList();

        return new TimeSeriesDto
        {
            Basis = basis, Granularity = granularity, Measure = measure, SplitBy = split,
            Series = series, Buckets = buckets, Total = buckets.Sum(b => b.Total), RecordsWithoutDate = noDate,
            FiscalYears = fySet.OrderByDescending(x => x).ToList(),
        };
    }

    public async Task<IReadOnlyList<NominationDto>> GetTimeSeriesDetailAsync(string? region, string basis, string granularity,
        string bucketKey, string? splitBy, string? series, CancellationToken ct = default)
    {
        basis = (basis ?? "completed").ToLowerInvariant();
        granularity = (granularity ?? "month").ToLowerInvariant();
        var split = (splitBy ?? "none").ToLowerInvariant();
        var (noms, segByAccount) = await LoadApprovedAsync(region, ct);

        var rows = new List<(DateOnly D, NominationDto N)>();
        foreach (var n in noms)
        {
            var d = BasisDate(n, basis);
            if (d is null) continue;
            var (_, key, _) = FiscalCalendar.Bucket(d.Value, granularity);
            if (!string.Equals(key, bucketKey, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(series) && !string.Equals(SeriesOf(n, split, segByAccount), series, StringComparison.OrdinalIgnoreCase)) continue;
            rows.Add((d.Value, n));
        }
        return rows.OrderBy(x => x.D).Select(x => x.N).ToList();
    }

    // Factory attainment: cumulative Target curve vs Completed (landed) + In-flight Total ACR, by fiscal month.
    public async Task<AttainmentDto> GetAttainmentAsync(string? region, int? fy, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fiscalYear = fy ?? FiscalCalendar.FiscalYear(today);
        var fyStart = new DateOnly(fiscalYear - 1, 7, 1);
        var fyEnd = new DateOnly(fiscalYear, 6, 30);

        // Annual ACR target from Configuration (AcrTarget<fiscalYear>); absent → prompt to set it.
        var setting = await _db.ApplicationSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == $"AcrTarget{fiscalYear}", ct);
        decimal annual = 0m;
        var targetSet = setting is not null && decimal.TryParse(setting.Value,
            System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out annual);

        var (noms, _) = await LoadApprovedAsync(region, ct);
        decimal Acr(NominationDto n) => n.TotalAcr ?? 0m;
        static int MonthIndex(DateOnly d) => (d.Month + 5) % 12 + 1; // Jul=1 … Jun=12

        // Per-fiscal-month increments: completed by ActualEndDate, in-flight by ApprovalDate (carryover → month 1).
        var completedInc = new decimal[13];
        var completedCnt = new int[13];
        var inflightInc = new decimal[13];
        foreach (var n in noms)
        {
            if (n.ActualEndDate is DateOnly aed && aed >= fyStart && aed <= fyEnd)
            {
                var mi = MonthIndex(aed);
                completedInc[mi] += Acr(n);
                completedCnt[mi] += 1;
            }
            else if (n.ActualEndDate is null)
            {
                var acr = Acr(n);
                if (acr <= 0) continue;
                DateOnly? apd = n.ApprovalDate;
                if (apd is DateOnly d && d > fyEnd) continue;          // approved after this FY → not yet in play
                var mi = apd is DateOnly a && a >= fyStart && a <= fyEnd ? MonthIndex(a) : 1; // carryover shows from Jul
                inflightInc[mi] += acr;
            }
        }

        var buckets = new List<AttainmentBucketDto>(12);
        decimal cumC = 0, cumI = 0;
        for (int m = 1; m <= 12; m++)
        {
            cumC += completedInc[m];
            cumI += inflightInc[m];
            var calMonth = (m + 5) % 12 + 1;
            var calYear = calMonth >= 7 ? fiscalYear - 1 : fiscalYear;
            var target = annual * m / 12m;
            buckets.Add(new AttainmentBucketDto
            {
                Key = $"{calYear}-{calMonth:00}",
                Label = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(calMonth),
                MonthIndex = m,
                Target = target,
                Completed = cumC,
                Inflight = cumI,
                Vtt = target - (cumC + cumI),
            });
        }

        var curIdx = today >= fyStart && today <= fyEnd ? MonthIndex(today) : 12;
        var cur = buckets[curIdx - 1];
        var completedCntYtd = completedCnt.Take(curIdx + 1).Sum();
        var avgSize = completedCntYtd > 0 ? cur.Completed / completedCntYtd : 0m;

        return new AttainmentDto
        {
            FiscalYear = fiscalYear,
            Label = FiscalCalendar.FyLabel(fiscalYear),
            Measure = "Total ACR",
            AnnualTarget = annual,
            TargetSet = targetSet,
            Buckets = buckets,
            CurrentMonthIndex = curIdx,
            TargetToDate = cur.Target,
            CompletedYtd = cur.Completed,
            InflightYtd = cur.Inflight,
            Vtt = cur.Vtt,
            AttainmentPct = annual > 0 ? (double)(cur.Completed / annual) * 100 : 0,
            PacePct = cur.Target > 0 ? (double)(cur.Completed / cur.Target) * 100 : 0,
            CompletedCount = completedCntYtd,
            AvgNominationSize = avgSize,
            NominationsNeeded = avgSize > 0 ? (double)(cur.Vtt / avgSize) : 0,
        };
    }

    private static DateOnly? BasisDate(NominationDto n, string basis) => basis switch
    {
        "nominated" => n.NominatedDate,
        "approved" => n.ApprovalDate,
        "started" => n.ActualStartDate,
        _ => n.ActualEndDate,
    };

    private static string SeriesOf(NominationDto n, string split, IReadOnlyDictionary<int, string> segByAccount) => split switch
    {
        "region" => string.IsNullOrWhiteSpace(n.Region) ? "Unspecified" : n.Region,
        "segment" => n.AccountId is int id && segByAccount.TryGetValue(id, out var s) && !string.IsNullOrWhiteSpace(s) ? s : "Unspecified",
        "path" => string.IsNullOrWhiteSpace(n.PrimaryMigrationPath) ? "Unspecified" : n.PrimaryMigrationPath!,
        "stage" => StageIndex(n.MigrationStatus) is int st ? StageLabels[st - 1] : "Unspecified",
        "status" => string.IsNullOrWhiteSpace(n.Status) ? "Unspecified" : n.Status,
        _ => "All",
    };

    private async Task<(List<NominationDto> Noms, Dictionary<int, string> SegByAccount)> LoadApprovedAsync(string? region, CancellationToken ct)
    {
        var all = await _nominations.GetAsync(region, null, ct);
        var noms = all.Where(n => string.Equals(n.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)).ToList();
        var accountIds = noms.Where(n => n.AccountId != null).Select(n => n.AccountId!.Value).Distinct().ToList();
        var segByAccount = await _db.Accounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.AccountId))
            .Select(a => new { a.AccountId, a.Segment })
            .ToDictionaryAsync(a => a.AccountId, a => a.Segment ?? "Unspecified", ct);
        return (noms, segByAccount);
    }
}
