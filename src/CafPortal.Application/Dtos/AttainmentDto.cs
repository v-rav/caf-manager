namespace CafPortal.Application.Dtos;

/// <summary>
/// Factory attainment for a fiscal year: cumulative ACR Target curve vs Completed (landed) + In-flight ACR by
/// fiscal month, with Velocity-to-Target (VTT) and the nominations still needed to close the gap.
/// </summary>
public class AttainmentDto
{
    public int FiscalYear { get; set; }
    public string Label { get; set; } = string.Empty;   // FY27
    public string Measure { get; set; } = "NNR ACR";     // which ACR field the sums use
    public decimal AnnualTarget { get; set; }
    public bool TargetSet { get; set; }                  // false → prompt the user to set it in Configuration

    public IReadOnlyList<AttainmentBucketDto> Buckets { get; set; } = [];

    // Headline, evaluated at the current fiscal month (or the full year when out of range).
    public int CurrentMonthIndex { get; set; }
    public decimal TargetToDate { get; set; }
    public decimal CompletedYtd { get; set; }
    public decimal InflightYtd { get; set; }
    public decimal Vtt { get; set; }                     // TargetToDate − (Completed + In-flight)
    public double AttainmentPct { get; set; }            // CompletedYtd ÷ AnnualTarget
    public double PacePct { get; set; }                  // CompletedYtd ÷ TargetToDate (on-pace signal)
    public int CompletedCount { get; set; }
    public decimal AvgNominationSize { get; set; }
    public double NominationsNeeded { get; set; }        // VTT ÷ AvgNominationSize (negative = ahead of plan)
}

public class AttainmentBucketDto
{
    public string Key { get; set; } = string.Empty;      // 2026-07
    public string Label { get; set; } = string.Empty;    // Jul
    public int MonthIndex { get; set; }                  // 1 = Jul … 12 = Jun
    public decimal Target { get; set; }                  // cumulative
    public decimal Completed { get; set; }               // cumulative
    public decimal Inflight { get; set; }                // cumulative
    public decimal Vtt { get; set; }
}
