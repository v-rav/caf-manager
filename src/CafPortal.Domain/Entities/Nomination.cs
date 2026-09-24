using CafPortal.Domain.Common;
using CafPortal.Domain.Enums;

namespace CafPortal.Domain.Entities;

public class Nomination : AuditableEntity
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? Technology { get; set; }
    public string Region { get; set; } = string.Empty;
    public NominationStatusType Status { get; set; } = NominationStatusType.Open;
    public DateOnly OpenedDate { get; set; }
    public string? Remarks { get; set; }

    /// <summary>Raw migration status from the nomination export (e.g. "Executing Migration").</summary>
    public string? MigrationStatus { get; set; }
    public string? CurrentState { get; set; }
    public string? SolutionArchitect { get; set; }
    public string? CftlPrimary { get; set; }
    public string? ProjectCoordinator { get; set; }

    /// <summary>Reason the nomination is blocked (required when Status = Blocked).</summary>
    public BlockerReasonType? BlockedReason { get; set; }
    /// <summary>Date the nomination entered a blocked/waiting state.</summary>
    public DateOnly? BlockedSince { get; set; }
    /// <summary>Next follow-up date for deferred/waiting items.</summary>
    public DateOnly? FollowUpDate { get; set; }

    public Account? Account { get; set; }
    public ICollection<WaveLink> WaveLinks { get; set; } = new List<WaveLink>();
}
