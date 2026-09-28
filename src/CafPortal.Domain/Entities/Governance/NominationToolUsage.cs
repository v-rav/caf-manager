using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Governance;

/// <summary>A Tool × Activity utilization fact on a nomination — "which capability was accelerated by which tool".
/// The junction that turns flat "tools used" into the Migration Capability Utilization story.</summary>
public class NominationToolUsage : AuditableEntity
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public int ToolId { get; set; }
    /// <summary>What the tool was used for; optional (a tool may be logged without pinning an activity).</summary>
    public int? ActivityId { get; set; }
    /// <summary>Optional migration stage (1–4) the usage happened in.</summary>
    public int? Stage { get; set; }
    public DateOnly? UsedOn { get; set; }
    public string? UsedBy { get; set; }
    public string? Notes { get; set; }
}
