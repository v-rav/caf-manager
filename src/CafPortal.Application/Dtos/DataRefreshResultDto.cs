namespace CafPortal.Application.Dtos;

public class DataRefreshResultDto
{
    public bool Success { get; set; }
    public DateTimeOffset StartedUtc { get; set; }
    public DateTimeOffset CompletedUtc { get; set; }
    public int ResourcesImported { get; set; }
    public int AccountsImported { get; set; }
    public int ResourceAccountLinks { get; set; }
    public int LeaveRecords { get; set; }
    public int EngagementRecords { get; set; }
    public int NominationRecords { get; set; }
    public int CapacityRowsRebuilt { get; set; }
    public List<string> Messages { get; set; } = new();
}
