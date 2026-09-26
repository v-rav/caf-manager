using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

/// <summary>
/// A reversible snapshot of an account removed from the master (currently: no-TPID rows that are
/// departments / app names / abbreviations, not canonical customers). Holds the full record plus
/// enough reference data (resource links, referencing nomination ids) to restore it via Unpark.
/// </summary>
public class ParkedAccount : AuditableEntity
{
    public int ParkedAccountId { get; set; }
    public int OriginalAccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;
    public string? Tpid { get; set; }
    public string? ExternalAccountId { get; set; }
    public string? Segment { get; set; }
    public string Region { get; set; } = string.Empty;
    public string? Status { get; set; }
    public bool StrategicFlag { get; set; }
    public int PriorityWeight { get; set; }
    public string? Aliases { get; set; }

    public string? ProjectManager { get; set; }
    public string? SolutionArchitect { get; set; }
    public string? Cftl { get; set; }
    public string? AccountOwner { get; set; }
    public string? CustomerPoc { get; set; }
    public string? BackupOwner { get; set; }

    /// <summary>Removed resource links, serialized as "resourceId:relationshipType" ';'-joined, for restore.</summary>
    public string? ResourceLinks { get; set; }
    /// <summary>Ids of nominations whose AccountId was nulled when parking, for restore.</summary>
    public string? NominationIds { get; set; }

    public string? Reason { get; set; }
    public DateTimeOffset ParkedUtc { get; set; }
}
