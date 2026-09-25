namespace CafPortal.Application.Dtos;

/// <summary>Freshness + row-count summary for the header banner.</summary>
public class DataStatusDto
{
    public DateTimeOffset? LastRefreshUtc { get; set; }
    public int Resources { get; set; }
    public int Accounts { get; set; }
    public int Nominations { get; set; }
    public int LeaveRecords { get; set; }
    public int PerformanceReviews { get; set; }
}
