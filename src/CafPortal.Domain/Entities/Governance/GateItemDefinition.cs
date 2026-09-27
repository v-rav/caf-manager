using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Governance;

public enum GateItemKind { Task = 0, Prerequisite = 1, Deliverable = 2, Approval = 3, Signoff = 4 }

/// <summary>A checklist item template within a gate. SubStage groups delivery items (Modernization/…/Deployment).</summary>
public class GateItemDefinition : AuditableEntity
{
    public int Id { get; set; }
    public int GateDefinitionId { get; set; }
    public GateDefinition? Gate { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public GateItemKind Kind { get; set; } = GateItemKind.Task;
    public string? SubStage { get; set; }              // e.g. Modernization/Containerization/IaC/CI-CD/Deployment
    public string ResponsibleRole { get; set; } = "SA"; // SA is always Accountable; this is who does it
    public bool Mandatory { get; set; }
    public int Order { get; set; }
    public bool Active { get; set; } = true;
}
