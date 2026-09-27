using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Governance;

/// <summary>A dated app-factory event on a nomination (kick-off, runbook shared, actual migration start, …).
/// Event-based dates so exceptions — e.g. kick-off happened but migration was deferred — are captured truthfully.</summary>
public class NominationMilestone : AuditableEntity
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public string MilestoneKey { get; set; } = string.Empty;
    public DateOnly OccurredOn { get; set; }
    public string? ToolUsed { get; set; }
    public string? Notes { get; set; }
    public string? RecordedBy { get; set; }
}
