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
    public string? Tpid { get; set; }
    public string? ExternalAccountId { get; set; }
    public string? Aliases { get; set; }
    public int ResourceCount { get; set; }

    public string? ProjectManager { get; set; }
    public string? SolutionArchitect { get; set; }
    public string? Cftl { get; set; }
    public string? AccountOwner { get; set; }
    public string? CustomerPoc { get; set; }
    public string? BackupOwner { get; set; }
}

public class AccountDetailDto : AccountDto
{
    public IReadOnlyList<ResourceSummaryDto> AssignedResources { get; set; } = Array.Empty<ResourceSummaryDto>();
    public IReadOnlyList<EngagementDto> RecentActivity { get; set; } = Array.Empty<EngagementDto>();
    public IReadOnlyList<OwnershipHistoryDto> OwnershipHistory { get; set; } = Array.Empty<OwnershipHistoryDto>();
}

public record OwnershipHistoryDto(int Id, string Role, string? PreviousOwner, string? NewOwner, DateOnly ChangedOn, string? Notes);

public record ResourceSummaryDto(int ResourceId, string Name, string Region, string Role, string? RelationshipType);

public record EngagementDto(int Id, DateOnly Date, string? ResourceName, string? MeetingName, double Duration, string? Region, string? Remarks);
