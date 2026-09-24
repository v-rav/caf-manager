namespace CafPortal.Application.Dtos;

public record LeaveDto(int Id, int ResourceId, string ResourceName, string Region, DateOnly LeaveDate, string LeaveType);

public class LeaveWindowDto
{
    public int WindowDays { get; set; }
    public int TotalLeaveDays { get; set; }
    public int DistinctResources { get; set; }
    public IReadOnlyList<LeaveDto> Items { get; set; } = Array.Empty<LeaveDto>();
}

/// <summary>A resource carrying active accounts who has upcoming leave — a coverage clash to watch.</summary>
public record LeaveClashDto(
    int ResourceId, string ResourceName, string Region, int ActiveAccounts,
    DateOnly NextLeaveStart, DateOnly NextLeaveEnd, int LeaveDaysInWindow);
