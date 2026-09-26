namespace CafPortal.Application.Dtos;

/// <summary>A configurable analytics time series: buckets over a date basis at a granularity, optionally split into series.</summary>
public class TimeSeriesDto
{
    public string Basis { get; set; } = string.Empty;       // nominated | approved | started | completed
    public string Granularity { get; set; } = string.Empty; // week | month | quarter | year
    public string Measure { get; set; } = string.Empty;     // count | acr | nnr | cores
    public string SplitBy { get; set; } = "none";           // none | region | segment | path | stage | status

    /// <summary>Series names present (one when SplitBy=none), for stacked/multi-series rendering.</summary>
    public IReadOnlyList<string> Series { get; set; } = [];
    public IReadOnlyList<TimeBucketDto> Buckets { get; set; } = [];
    public double Total { get; set; }
    /// <summary>Approved nominations skipped because the chosen basis date is missing (coverage signal).</summary>
    public int RecordsWithoutDate { get; set; }
    /// <summary>Fiscal years present in the (region-scoped, basis-dated) data, descending — for the FY filter.</summary>
    public IReadOnlyList<int> FiscalYears { get; set; } = [];
}

public class TimeBucketDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>Per-series values, aligned to <see cref="TimeSeriesDto.Series"/>.</summary>
    public IReadOnlyList<NameValueDto> Values { get; set; } = [];
    public double Total { get; set; }
}
