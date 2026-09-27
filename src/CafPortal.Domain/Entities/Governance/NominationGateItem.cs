using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Governance;

public enum GateItemStatus { Pending = 0, Done = 1, NotApplicable = 2 }

/// <summary>Per-nomination state of a gate checklist item (materialized from the template on first use).</summary>
public class NominationGateItem : AuditableEntity
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public int GateItemDefinitionId { get; set; }
    public GateItemDefinition? ItemDefinition { get; set; }
    public GateItemStatus Status { get; set; } = GateItemStatus.Pending;
    public string? Owner { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public string? Ref { get; set; }
    public string? Notes { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedUtc { get; set; }
}
