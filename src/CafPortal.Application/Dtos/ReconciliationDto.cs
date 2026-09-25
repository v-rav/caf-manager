namespace CafPortal.Application.Dtos;

/// <summary>
/// One resource→account mapping row with data-quality flags, used to review the capacity source
/// before cleansing. Read-only; nothing here mutates data.
/// </summary>
public record ReconciliationRowDto(
    int ResourceId,
    string ResourceName,
    string ResourceRegion,
    int AccountId,
    string AccountName,
    string? Tpid,
    string? Segment,
    string? RelationshipType,
    bool InMaster,
    bool InFlight,
    string MatchState,          // Master | SuggestMerge | Orphan
    int? SuggestedAccountId,
    string? SuggestedAccountName,
    string? SuggestedTpid,
    string UtilizationEffect);  // Keep | Drop

public class ReconciliationSummaryDto
{
    public int TotalLinks { get; set; }
    public int Resources { get; set; }
    public int LinkedAccounts { get; set; }
    public int Master { get; set; }
    public int SuggestMerge { get; set; }
    public int Orphan { get; set; }
    public int Keep { get; set; }
    public int Drop { get; set; }
    public int InFlightAccounts { get; set; }
}

public class ReconciliationReportDto
{
    public ReconciliationSummaryDto Summary { get; set; } = new();
    public IReadOnlyList<ReconciliationRowDto> Rows { get; set; } = Array.Empty<ReconciliationRowDto>();
}

/// <summary>One assignment the seeder would create (or found already present).</summary>
public record SeedAssignmentRowDto(
    int NominationId,
    string? AccountName,
    int ResourceId,
    string ResourceName,
    string Role,
    bool AlreadyExisted);

/// <summary>Outcome of seeding NominationResource assignments from the in-flight resource→account links.</summary>
public class SeedAssignmentResultDto
{
    public bool Applied { get; set; }
    public int InFlightLinks { get; set; }
    public int Candidates { get; set; }
    public int WouldCreate { get; set; }
    public int Created { get; set; }
    public int RemovedSeed { get; set; }
    public int SaCreated { get; set; }
    public int EngineerCreated { get; set; }
    public int SaUnmatchedWaves { get; set; }
    public int SkippedExisting { get; set; }
    public int ResourcesAffected { get; set; }
    public int NominationsAffected { get; set; }
    public IReadOnlyList<SeedAssignmentRowDto> Sample { get; set; } = Array.Empty<SeedAssignmentRowDto>();
}
