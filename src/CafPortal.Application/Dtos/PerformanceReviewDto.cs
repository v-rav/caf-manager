namespace CafPortal.Application.Dtos;

public class PerformanceReviewDto
{
    public int Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? ReportingManager { get; set; }
    public string Region { get; set; } = string.Empty;
    public DateOnly ReviewDate { get; set; }

    public double? CommunicationVerbal { get; set; }
    public double? CommunicationWritten { get; set; }
    public double? Attitude { get; set; }
    public double? ProcessUnderstanding { get; set; }
    public double? OfferingUnderstanding { get; set; }

    /// <summary>Average of the scored dimensions; null when the person has not been reviewed yet.</summary>
    public double? Score { get; set; }
    public bool Pending { get; set; }

    /// <summary>Training areas suggested from dimensions below the configured threshold.</summary>
    public IReadOnlyList<string> TrainingNeeds { get; set; } = Array.Empty<string>();

    /// <summary>Score change vs the person's previous review (positive = improving).</summary>
    public double? TrendDelta { get; set; }
    public double? PreviousScore { get; set; }
    public int ReviewCount { get; set; }

    public string? Comments { get; set; }
}

/// <summary>Add or re-score a person. Each save is a dated snapshot for trend tracking.</summary>
public class PerformanceReviewUpsertDto
{
    public string PersonName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? ReportingManager { get; set; }
    public string? Region { get; set; }
    public DateOnly? ReviewDate { get; set; }

    public double? CommunicationVerbal { get; set; }
    public double? CommunicationWritten { get; set; }
    public double? Attitude { get; set; }
    public double? ProcessUnderstanding { get; set; }
    public double? OfferingUnderstanding { get; set; }

    public string? Comments { get; set; }
}
