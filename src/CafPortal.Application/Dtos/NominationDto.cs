namespace CafPortal.Application.Dtos;

public class NominationDto
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? ShortName { get; set; }
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

    // Strategic governance (P3). Classification defaults to Standard Factory when unset.
    public string Classification { get; set; } = "Standard Factory";
    public string? VelocityImpact { get; set; }
    /// <summary>GHCP adoption maturity 0–7 (A.10); null means not yet assessed.</summary>
    public int? GhcpAdoptionLevel { get; set; }
    /// <summary>True when classification is anything other than Standard Factory (strategic investment).</summary>
    public bool IsStrategic { get; set; }
    /// <summary>Days since nomination/open — the strategic-pilot running clock.</summary>
    public int DaysInFlight { get; set; }
    /// <summary>"", "Green", "Amber", "Red" or "Exec" from days-in-flight against the 60/90/120 thresholds.</summary>
    public string StrategicTier { get; set; } = string.Empty;

    // Migration Success Index (0–100) and its components.
    public int MsiScore { get; set; }
    public string MsiBand { get; set; } = "Red";
    public int MsiReadiness { get; set; }
    public int MsiScope { get; set; }
    public int MsiDelivery { get; set; }
    public int MsiRisk { get; set; }
    public int MsiGhcp { get; set; }
    public int MsiSignoff { get; set; }
    public string? BlockedReason { get; set; }
    public DateOnly? BlockedSince { get; set; }
    public DateOnly? FollowUpDate { get; set; }

    // Enrichment from "Summary of All Offerings" (TPID + Task Id).
    public string? PrimaryMigrationPath { get; set; }
    public string? PartnerName { get; set; }
    public int? TotalCores { get; set; }
    public bool? IsToolAttached { get; set; }
    public bool? IsAutomationUsed { get; set; }
    public string? ModeOfAccess { get; set; }
    public decimal? TotalAcr { get; set; }
    public decimal? NnrAcr { get; set; }
    public DateOnly? NominatedDate { get; set; }
    public DateOnly? ApprovalDate { get; set; }
    public DateOnly? ActualStartDate { get; set; }
    public DateOnly? ActualEndDate { get; set; }
    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public int? TotalDays { get; set; }

    /// <summary>Whole days since last update. Drives stale detection.</summary>
    public int DaysSinceUpdate { get; set; }
    /// <summary>Stage age minus time spent under a clock-stopping blocker. Drives the SLA tier.</summary>
    public int EffectiveAgeDays { get; set; }
    /// <summary>True when an open blocker is currently stopping the SLA clock.</summary>
    public bool ClockStopped { get; set; }
    /// <summary>Count of open blockers on this nomination.</summary>
    public int OpenBlockerCount { get; set; }
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

    // Key milestone dates + lag (portal-entered). Surfaces the lift-n-shift exception where kick-off
    // and actual migration start are separate events. Null when the milestone hasn't been recorded.
    public DateOnly? KickoffDate { get; set; }
    public DateOnly? ActualMigrationStartDate { get; set; }
    /// <summary>Days between kick-off and actual migration start; null unless both milestones exist.</summary>
    public int? KickoffToStartLagDays { get; set; }
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
    public string? Classification { get; set; }
    public string? VelocityImpact { get; set; }
    public int? GhcpAdoptionLevel { get; set; }
    public string? ShortName { get; set; }
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
