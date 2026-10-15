namespace CafPortal.Application.Dtos;

/// <summary>Velocity page filters (region is passed separately). Empty = all.</summary>
public record VelocityFilter(int? Fy = null, bool ExcludeStrategic = false, string? Segment = null, string? Path = null,
    string? Sa = null, string? Pm = null, int? Stage = null, string? Set = null, string? Classification = null);

/// <summary>FDO-style "Stage Duration vs SLA (Avg. No. of Business Days)": per-stage target vs average actual.</summary>
public class StageVelocityDto
{
    public StageVelocitySet InFlight { get; set; } = new();
    public StageVelocitySet Completed { get; set; } = new();
    /// <summary>Completed-wave velocity per quarter of the selected (else current) fiscal year by actual end date.</summary>
    public List<VelocityPeriod> ByQuarter { get; set; } = new();
    /// <summary>Last month and current month.</summary>
    public List<VelocityPeriod> ByMonth { get; set; } = new();
    /// <summary>Per-wave rows behind the In-flight + Completed sets.</summary>
    public List<VelocityWave> Waves { get; set; } = new();
    public VelocityOptions Options { get; set; } = new();
}

public class VelocityWave
{
    public int NominationId { get; set; }
    public string? AccountName { get; set; }
    public string? Tpid { get; set; }
    public string Set { get; set; } = string.Empty;
    public int? StageIndex { get; set; }
    public string? Segment { get; set; }
    public string? Path { get; set; }
    public string? Sa { get; set; }
    public string? Pm { get; set; }
    public string Classification { get; set; } = string.Empty;
    public int?[] StageDays { get; set; } = new int?[4];
    public int? TotalDays { get; set; }
    public int? TargetDays { get; set; }
    public int? VelocityPct { get; set; }
    public string? Tone { get; set; }
    public DateOnly? ActualEndDate { get; set; }
}

/// <summary>Faceted filter vocabularies: each list honours every other active filter (unique, case-insensitive).</summary>
public class VelocityOptions
{
    public List<string> Segments { get; set; } = new();
    public List<string> Paths { get; set; } = new();
    public List<string> Sas { get; set; } = new();
    public List<string> Pms { get; set; } = new();
    public List<int> Stages { get; set; } = new();
    public List<string> Classifications { get; set; } = new();
    public int NonStandard { get; set; }
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
