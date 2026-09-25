using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

/// <summary>A dated performance-review snapshot for a person. Re-scored at intervals to track improvement.</summary>
public class PerformanceReview : AuditableEntity
{
    public int Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? ReportingManager { get; set; }
    public string Region { get; set; } = "EMEA";
    public DateOnly ReviewDate { get; set; }

    // Review dimensions (1–5). Null = not yet scored for this snapshot.
    public double? CommunicationVerbal { get; set; }
    public double? CommunicationWritten { get; set; }
    public double? Attitude { get; set; }
    public double? ProcessUnderstanding { get; set; }
    public double? OfferingUnderstanding { get; set; }

    public string? Comments { get; set; }
}
