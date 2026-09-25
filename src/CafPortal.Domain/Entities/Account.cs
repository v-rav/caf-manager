using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

public class Account : AuditableEntity
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? Status { get; set; }
    public bool StrategicFlag { get; set; }
    public int PriorityWeight { get; set; } = 1;

    /// <summary>Sales/coverage segment (e.g. Strategic, Upper Majors).</summary>
    public string? Segment { get; set; }

    /// <summary>External master keys from the nomination export (TPID and Account ID).</summary>
    public string? Tpid { get; set; }
    public string? ExternalAccountId { get; set; }

    /// <summary>Semicolon-separated alternate account-name spellings used to match this account by name.</summary>
    public string? Aliases { get; set; }

    /// <summary>Ownership matrix (reflects FDO roles) plus a backup owner for SPOF/handover cover.</summary>
    public string? ProjectManager { get; set; }
    public string? SolutionArchitect { get; set; }
    public string? Cftl { get; set; }
    public string? AccountOwner { get; set; }
    public string? CustomerPoc { get; set; }
    public string? BackupOwner { get; set; }

    public ICollection<ResourceAccount> ResourceAccounts { get; set; } = new List<ResourceAccount>();
    public ICollection<EngagementFact> EngagementFacts { get; set; } = new List<EngagementFact>();
    public ICollection<OwnershipHistory> OwnershipHistory { get; set; } = new List<OwnershipHistory>();
}
