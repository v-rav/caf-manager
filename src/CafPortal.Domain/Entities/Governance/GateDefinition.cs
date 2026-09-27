using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Governance;

/// <summary>A governance gate template (G1–G8). Editable in Configuration.</summary>
public class GateDefinition : AuditableEntity
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;   // G1..G8
    public string Name { get; set; } = string.Empty;
    public string? ExitCriteria { get; set; }
    public int Order { get; set; }
    public int Weight { get; set; }                    // relative weight for Readiness Compliance %
    public string OwnerRole { get; set; } = "SA";
    public bool Active { get; set; } = true;

    public ICollection<GateItemDefinition> Items { get; set; } = new List<GateItemDefinition>();
}
