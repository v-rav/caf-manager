namespace CafPortal.Domain.Entities.Governance;

/// <summary>
/// Portal-owned ACR recovery claim. Freezes the baseline cores/ACR when a nomination is flagged for
/// under-captured ACR, tracks SA notification, and books the realized delta once a later FDO import
/// raises the value — attributing recovered ACR to the system analysis. Never touched by imports.
/// </summary>
public class AcrRecoveryEntry
{
    public int Id { get; set; }
    public int NominationId { get; set; }
    public string? GapType { get; set; }

    // Frozen "before" at flag time (survives FDO re-import).
    public int? BaselineCores { get; set; }
    public decimal? BaselineAcr { get; set; }
    // The system's projection at flag time.
    public int? RecommendedCores { get; set; }
    public decimal? RecommendedAcr { get; set; }

    public DateTime FlaggedUtc { get; set; } = DateTime.UtcNow;
    public string? FlaggedBy { get; set; }
    public DateTime? NotifiedUtc { get; set; }
    public string? NotifiedBy { get; set; }

    // Booked when a later import raises the value above baseline.
    public int? RealizedCores { get; set; }
    public decimal? RealizedAcr { get; set; }
    public DateTime? RealizedUtc { get; set; }
    public decimal? RecoveredAcr { get; set; }

    /// <summary>Flagged · Notified · Realized · Closed.</summary>
    public string Status { get; set; } = "Flagged";
    public DateTime? ClosedUtc { get; set; }
    public string? Note { get; set; }

    public Nomination? Nomination { get; set; }
}
