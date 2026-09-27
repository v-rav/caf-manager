using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Governance;

/// <summary>A blocker/dependency on a nomination (optionally on a gate item). Clock-stopped time is excluded from SLA/velocity.</summary>
public class NominationBlocker : AuditableEntity
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public int? GateItemDefinitionId { get; set; }     // optional: the stuck checklist item
    public string Category { get; set; } = string.Empty;
    public bool ClockStopped { get; set; }
    public string? Owner { get; set; }
    public DateTime BlockedSinceUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedResolutionUtc { get; set; }
    public DateTime? ResolvedUtc { get; set; }
    public string? Notes { get; set; }
    public string? RaisedBy { get; set; }
    public string? ResolvedBy { get; set; }
}
