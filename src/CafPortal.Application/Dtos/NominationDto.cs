namespace CafPortal.Application.Dtos;

public class NominationDto
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? Technology { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly OpenedDate { get; set; }
    public string? Remarks { get; set; }

    public string? MigrationStatus { get; set; }
    public string? CurrentState { get; set; }
    public string? SolutionArchitect { get; set; }
    public string? CftlPrimary { get; set; }
    public string? ProjectCoordinator { get; set; }
}
