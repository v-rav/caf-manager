namespace CafPortal.Application.Dtos;

/// <summary>FDO-style "Stage Duration vs SLA (Avg. No. of Business Days)": per-stage target vs average actual.</summary>
public class StageVelocityDto
{
    public StageVelocitySet InFlight { get; set; } = new();
    public StageVelocitySet Completed { get; set; } = new();
    /// <summary>Completed-wave velocity per quarter of the current fiscal year (Q1–Q4) by actual end date.</summary>
    public List<VelocityPeriod> ByQuarter { get; set; } = new();
    /// <summary>Last month and current month.</summary>
    public List<VelocityPeriod> ByMonth { get; set; } = new();
}

public class VelocityPeriod
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public bool IsFuture { get; set; }
    public int Count { get; set; }
    public int? AvgDays { get; set; }
    public int Target { get; set; }
    public int? VelocityPct { get; set; }
    public string? Tone { get; set; }
}

public class StageVelocitySet
{
    /// <summary>Nominations with an FDO stage-day breakdown in this set.</summary>
    public int Count { get; set; }
    public List<StageVelocityRow> Stages { get; set; } = new();
    public int OverallTarget { get; set; }
    public int? OverallActual { get; set; }
    /// <summary>Actual − target (positive = days over, negative = days under).</summary>
    public int? OverallOver { get; set; }
    /// <summary>Target ÷ actual as a whole % (100 = on target, higher = faster).</summary>
    public int? OverallVelocityPct { get; set; }
    public string? OverallVelocityTone { get; set; }
}

public class StageVelocityRow
{
    public int Stage { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Target { get; set; }
    public int? Actual { get; set; }
    public int? Over { get; set; }
    public int Count { get; set; }
}
