using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>
/// Migration analytics over Approved nominations (region-scoped). Reuses INominationService so it inherits
/// the same stage/stale/wave derivations and the Offerings-enriched fields; joins Segment by AccountId.
/// </summary>
public class AnalyticsService(INominationService nominations, IApplicationDbContext db) : IAnalyticsService
{
    private readonly INominationService _nominations = nominations;
    private readonly IApplicationDbContext _db = db;

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
        };
        return dto;
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
