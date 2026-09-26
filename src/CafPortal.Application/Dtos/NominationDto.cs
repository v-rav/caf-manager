namespace CafPortal.Application.Dtos;

public class NominationDto
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? Tpid { get; set; }
    public string? Technology { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly OpenedDate { get; set; }
    public string? Remarks { get; set; }

    public string? MigrationStatus { get; set; }
    public string? ApprovalStatus { get; set; }
    /// <summary>Days spent in the current migration stage (from the stage's day-count column).</summary>
    public int? StageAgeDays { get; set; }
    public string? CurrentState { get; set; }
    public string? SolutionArchitect { get; set; }
    public string? CftlPrimary { get; set; }
    public string? ProjectCoordinator { get; set; }

    public string? BlockedReason { get; set; }
    public DateOnly? BlockedSince { get; set; }
    public DateOnly? FollowUpDate { get; set; }

    // Enrichment from "Summary of All Offerings" (TPID + Task Id).
    public string? PrimaryMigrationPath { get; set; }
    public string? PartnerName { get; set; }
    public int? TotalCores { get; set; }
    public bool? IsToolAttached { get; set; }
    public bool? IsAutomationUsed { get; set; }

    /// <summary>Whole days since last update. Drives stale detection.</summary>
    public int DaysSinceUpdate { get; set; }
    /// <summary>"", "Warn", "Escalate" or "Defer" per the Day 3/5/10 cadence.</summary>
    public string StaleTier { get; set; } = string.Empty;

    // Wave links are informational — no wave type is mandatory. These flags surface what's present so
    // records with no waves can be found and enriched; they are never a validation error.
    public bool DbLinked { get; set; }
    public bool AlzLinked { get; set; }
    public bool SecurityLinked { get; set; }
    public int WaveCount { get; set; }
    /// <summary>True when no waves are linked yet — the "review &amp; associate waves" signal.</summary>
    public bool NoWavesLinked { get; set; }

    public IReadOnlyList<WaveLinkDto> Waves { get; set; } = Array.Empty<WaveLinkDto>();

    /// <summary>Resources assigned to this nomination (operational staffing, separate from account capacity).</summary>
    public IReadOnlyList<NominationResourceDto> AssignedResources { get; set; } = Array.Empty<NominationResourceDto>();
    public int AssignedResourceCount { get; set; }
}

/// <summary>A resource assigned to a nomination with a delivery role.</summary>
public record NominationResourceDto(int ResourceId, string Name, string Region, string? Role);

/// <summary>Payload to assign (or re-role) a resource on a nomination.</summary>
public class AssignResourceDto
{
    public int ResourceId { get; set; }
    public string? Role { get; set; }
}

/// <summary>Update payload for a nomination (status/blocker/follow-up managed in the portal).</summary>
public class NominationUpdateDto
{
    public string Status { get; set; } = "Open";
    public string? MigrationStatus { get; set; }
    public string? BlockedReason { get; set; }
    public DateOnly? BlockedSince { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? Remarks { get; set; }
    public string? ProjectCoordinator { get; set; }
    public string? CftlPrimary { get; set; }
    public string? SolutionArchitect { get; set; }
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
