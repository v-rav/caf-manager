namespace CafPortal.Application.Common;

/// <summary>
/// Fiscal calendar. FY starts Jul 1 and is labeled by the calendar year it ends in
/// (Microsoft convention): FY27 = Jul 1 2026 → Jun 30 2027. Single source of FY logic.
/// </summary>
public static class FiscalCalendar
{
    public static int FiscalYear(DateOnly d) => d.Month >= 7 ? d.Year + 1 : d.Year;

    /// <summary>Fiscal quarter 1–4: Q1 Jul–Sep · Q2 Oct–Dec · Q3 Jan–Mar · Q4 Apr–Jun.</summary>
    public static int FiscalQuarter(DateOnly d) => (d.Month + 5) % 12 / 3 + 1;

    public static string FyLabel(int fiscalYear) => $"FY{fiscalYear % 100:00}";

    /// <summary>Sort key (chronological), stable bucket key, and display label for a date at a granularity.</summary>
    public static (long Sort, string Key, string Label) Bucket(DateOnly d, string granularity) => granularity switch
    {
        "week" => WeekBucket(d),
        "quarter" => QuarterBucket(d),
        "year" => YearBucket(d),
        _ => MonthBucket(d),
    };

    private static (long, string, string) WeekBucket(DateOnly d)
    {
        var dt = d.ToDateTime(TimeOnly.MinValue);
        var year = System.Globalization.ISOWeek.GetYear(dt);
        var week = System.Globalization.ISOWeek.GetWeekOfYear(dt);
        return (year * 100L + week, $"{year}-W{week:00}", $"{year} W{week:00}");
    }

    private static (long, string, string) MonthBucket(DateOnly d)
    {
        var mon = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(d.Month);
        return (d.Year * 100L + d.Month, $"{d.Year}-{d.Month:00}", $"{mon} {d.Year}");
    }

    private static (long, string, string) QuarterBucket(DateOnly d)
    {
        var fy = FiscalYear(d);
        var q = FiscalQuarter(d);
        return (fy * 10L + q, $"{fy}-Q{q}", $"{FyLabel(fy)} Q{q}");
    }

    private static (long, string, string) YearBucket(DateOnly d)
    {
        var fy = FiscalYear(d);
        return (fy, $"FY{fy}", FyLabel(fy));
    }
}
