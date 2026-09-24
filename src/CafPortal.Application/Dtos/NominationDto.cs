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

    public string? BlockedReason { get; set; }
    public DateOnly? BlockedSince { get; set; }
    public DateOnly? FollowUpDate { get; set; }

    /// <summary>Whole days since last update. Drives stale detection.</summary>
    public int DaysSinceUpdate { get; set; }
    /// <summary>"", "Warn", "Escalate" or "Defer" per the Day 3/5/10 cadence.</summary>
    public string StaleTier { get; set; } = string.Empty;

    public IReadOnlyList<WaveLinkDto> Waves { get; set; } = Array.Empty<WaveLinkDto>();
}

/// <summary>Update payload for a nomination (status/blocker/follow-up managed in the portal).</summary>
public class NominationUpdateDto
{
    public string Status { get; set; } = "Open";
    public string? BlockedReason { get; set; }
    public DateOnly? BlockedSince { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? Remarks { get; set; }
}

public class WaveLinkDto
{
    public int Id { get; set; }
    public string WaveType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class WaveLinkUpsertDto
{
    public string WaveType { get; set; } = "App";
    public string Reference { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
