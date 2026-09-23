using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

/// <summary>A person tracked by the portal. Capacity limit is per-resource and defaults from configuration.</summary>
public class Resource : AuditableEntity
{
    public int ResourceId { get; set; }
    public string? Psid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PrimarySkill { get; set; }
    public string? Skills { get; set; }
    public double ExperienceYears { get; set; }
    public string? Status { get; set; }
    public bool DedicatedFlag { get; set; }
    public int CapacityLimit { get; set; } = 5;
    public bool ActiveFlag { get; set; } = true;

    public ICollection<ResourceAccount> ResourceAccounts { get; set; } = new List<ResourceAccount>();
    public ICollection<LeaveFact> LeaveFacts { get; set; } = new List<LeaveFact>();
    public ICollection<EngagementFact> EngagementFacts { get; set; } = new List<EngagementFact>();
}
