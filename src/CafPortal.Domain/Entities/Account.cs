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

    public ICollection<ResourceAccount> ResourceAccounts { get; set; } = new List<ResourceAccount>();
    public ICollection<EngagementFact> EngagementFacts { get; set; } = new List<EngagementFact>();
}
