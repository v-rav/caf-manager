namespace CafPortal.Domain.Entities.Governance;

/// <summary>Append-only audit event on a nomination (gate-item changes, blockers). Never mutated.</summary>
public class NominationEvent
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public int? GateItemDefinitionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ByUser { get; set; }
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
}
