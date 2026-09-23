namespace CafPortal.Application.Dtos;

public class AccountDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? Status { get; set; }
    public bool StrategicFlag { get; set; }
    public int PriorityWeight { get; set; }
    public string? Segment { get; set; }
    public int ResourceCount { get; set; }
}

public class AccountDetailDto : AccountDto
{
    public IReadOnlyList<ResourceSummaryDto> AssignedResources { get; set; } = Array.Empty<ResourceSummaryDto>();
    public IReadOnlyList<EngagementDto> RecentActivity { get; set; } = Array.Empty<EngagementDto>();
}

public record ResourceSummaryDto(int ResourceId, string Name, string Region, string Role, string? RelationshipType);

public record EngagementDto(int Id, DateOnly Date, string? ResourceName, string? MeetingName, double Duration, string? Region, string? Remarks);
