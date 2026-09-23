namespace CafPortal.Application.Dtos;

public class ResourceDto
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
    public int CapacityLimit { get; set; }
    public bool ActiveFlag { get; set; }
    public int AccountCount { get; set; }
    public double UtilizationPercent { get; set; }
    public string CapacityStatus { get; set; } = string.Empty;
    public bool OnLeaveToday { get; set; }
}

public class ResourceDetailDto : ResourceDto
{
    public IReadOnlyList<AccountSummaryDto> Accounts { get; set; } = Array.Empty<AccountSummaryDto>();
    public IReadOnlyList<LeaveDto> UpcomingLeave { get; set; } = Array.Empty<LeaveDto>();
}

public record AccountSummaryDto(int AccountId, string AccountName, string Region, bool StrategicFlag, string? RelationshipType);
