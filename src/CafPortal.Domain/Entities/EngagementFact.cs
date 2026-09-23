using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

public class EngagementFact : AuditableEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int? ResourceId { get; set; }
    public int? AccountId { get; set; }
    public string? MeetingName { get; set; }
    public double Duration { get; set; }
    public string? Region { get; set; }
    public string? Remarks { get; set; }

    public Resource? Resource { get; set; }
    public Account? Account { get; set; }
}
