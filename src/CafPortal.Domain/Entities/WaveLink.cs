using CafPortal.Domain.Common;
using CafPortal.Domain.Enums;

namespace CafPortal.Domain.Entities;

/// <summary>Links a nomination (and its account) to a related wave — App, DB, Security, Landing Zone, etc.</summary>
public class WaveLink : AuditableEntity
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public int? AccountId { get; set; }
    public WaveType WaveType { get; set; }

    /// <summary>Free reference to the linked wave/item (name, ID, or URL).</summary>
    public string Reference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    /// <summary>Origin of the link: "FDO" (imported, refreshed each drop) or "Portal" (manually added).</summary>
    public string? Source { get; set; }

    public Nomination? Nomination { get; set; }
    public Account? Account { get; set; }
}
