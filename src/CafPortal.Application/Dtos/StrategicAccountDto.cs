namespace CafPortal.Application.Dtos;

public class StrategicAccountDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int PriorityWeight { get; set; }
    public bool RiskFlag { get; set; }
    public bool ExecutiveVisibilityFlag { get; set; }
    public int AssignedResourceCount { get; set; }
    public int RecentActivityCount { get; set; }
    public DateOnly? LastActivityDate { get; set; }
    /// <summary>Green when covered + active, Amber when thin coverage, Red when uncovered or risk-flagged.</summary>
    public string RiskIndicator { get; set; } = "Green";
}
