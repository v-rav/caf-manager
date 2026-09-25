using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

/// <summary>Operational staffing: a resource assigned to a nomination (migration engagement) with a delivery role.</summary>
public class NominationResource : AuditableEntity
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public int ResourceId { get; set; }

    /// <summary>Delivery role on this nomination (e.g. Solution Architect, Migration Engineer, DevOps Engineer).</summary>
    public string? Role { get; set; }

    /// <summary>Origin: "Seed" (reconciliation seed, re-created on re-seed) or "Portal" (manual, preserved).</summary>
    public string? Source { get; set; }

    public Nomination? Nomination { get; set; }
    public Resource? Resource { get; set; }
}
