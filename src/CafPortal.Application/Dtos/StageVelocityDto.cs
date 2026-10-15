namespace CafPortal.Application.Dtos;

/// <summary>FDO-style "Stage Duration vs SLA (Avg. No. of Business Days)": per-stage target vs average actual.</summary>
public class StageVelocityDto
{
    public StageVelocitySet InFlight { get; set; } = new();
    public StageVelocitySet Completed { get; set; } = new();
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
