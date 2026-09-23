using System.ComponentModel.DataAnnotations;

namespace CafPortal.Application.Dtos;

/// <summary>Create/update payload for a resource.</summary>
public class ResourceUpsertDto
{
    public string? Psid { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    [Required] public string Region { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = string.Empty;
    public string? PrimarySkill { get; set; }
    public string? Skills { get; set; }
    public double ExperienceYears { get; set; }
    public string? Status { get; set; } = "Active";
    public bool DedicatedFlag { get; set; }
    public int CapacityLimit { get; set; } = 5;
    public bool ActiveFlag { get; set; } = true;
}

/// <summary>Create/update payload for an account.</summary>
public class AccountUpsertDto
{
    [Required] public string AccountName { get; set; } = string.Empty;
    [Required] public string Region { get; set; } = string.Empty;
    public string? Status { get; set; } = "Active";
    public bool StrategicFlag { get; set; }
    public int PriorityWeight { get; set; } = 1;
    public string? Segment { get; set; }
}

/// <summary>Create/update payload for a leave record.</summary>
public class LeaveUpsertDto
{
    [Required] public int ResourceId { get; set; }
    [Required] public DateOnly LeaveDate { get; set; }
    [Required] public string LeaveType { get; set; } = "Leave";
}

/// <summary>Assigns an account to a resource (resource ↔ account mapping).</summary>
public class AssignAccountDto
{
    [Required] public int AccountId { get; set; }
    public string RelationshipType { get; set; } = "Primary";
}
