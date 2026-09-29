namespace CafPortal.Application.Dtos;

public record GateItemDto(
    int ItemDefId, string Key, string Label, string Kind, string? SubStage, string ResponsibleRole,
    bool Mandatory, int Order, string Status, string? Owner, DateTime? CompletedUtc, string? Ref, string? Notes,
    string? UpdatedBy, DateTime? UpdatedUtc, bool Blocked);

public record GateDto(
    string Key, string Name, string? ExitCriteria, int Order, int Weight, string Status, int PercentComplete,
    IReadOnlyList<GateItemDto> Items);

public record NominationGovernanceDto(
    int NominationId, string? Account, string? Tpid, string? Classification, string? CurrentGateKey,
    int CompliancePercent, IReadOnlyList<GateDto> Gates, IReadOnlyList<BlockerDto> Blockers,
    int MsiScore, string MsiBand,
    int? Stage = null, string? Pm = null, string? Cftl = null, string? Sa = null, int? AgeDays = null, int ClockStoppedDays = 0,
    string? ShortName = null, IReadOnlyList<MilestoneDto>? Milestones = null,
    IReadOnlyList<ToolUsageDto>? ToolUsages = null);

public record MilestoneDto(int Id, string MilestoneKey, DateOnly OccurredOn, string? ToolUsed, string? Notes, string? RecordedBy);

public record MilestoneUpsert(string MilestoneKey, DateOnly OccurredOn, string? ToolUsed, string? Notes);

// Migration Capability Utilization: masters + the per-nomination Tool × Activity fact.
public record MigrationToolDto(int Id, string Name, string Category, string? Vendor, int SortOrder, bool Active,
    IReadOnlyList<int> SupportedActivityIds);

public record MigrationActivityDto(int Id, string Name, string? Stage, int SortOrder, bool Active);

public record ToolUsageDto(
    int Id, int NominationId, int ToolId, string ToolName, string ToolCategory, string? ToolVendor,
    int? ActivityId, string? ActivityName, string? ActivityStage, int? Stage, DateOnly? UsedOn, string? UsedBy, string? Notes);

public record ToolUsageUpsert(int ToolId, int? ActivityId, int? Stage, DateOnly? UsedOn, string? Notes);

/// <summary>Captures corrected cores/ACR onto a nomination from the ACR core-capture worklist (audited).</summary>
public record AcrApplyRequest(int Cores, decimal? Acr, string? Reason);

/// <summary>Leadership rollup: which capabilities were accelerated by which tools (distinct nominations).</summary>
public record CapabilityUtilizationDto(
    int TotalUsages, int NominationsWithUsage,
    IReadOnlyList<NameValueDto> ByTool, IReadOnlyList<NameValueDto> ByCategory,
    IReadOnlyList<NameValueDto> ByActivity, IReadOnlyList<CapabilityOutcomeDto> MostUsedToolPerActivity);

public record CapabilityOutcomeDto(string Activity, string Tool, int Nominations);

public record BlockerDto(
    int Id, int NominationId, string? Account, int? GateItemDefId, string Category, bool ClockStopped, string? Owner,
    DateTime BlockedSinceUtc, DateTime? ExpectedResolutionUtc, DateTime? ResolvedUtc, string? Notes, int DaysBlocked,
    string? RaisedBy);

public record UpdateGateItemRequest(string Status, string? Owner, string? Ref, string? Notes);

public record RaiseBlockerRequest(string Category, bool ClockStopped, string? Owner, DateTime? ExpectedResolutionUtc, string? Notes, int? GateItemDefId);

public record NominationEventDto(
    int Id, int NominationId, int? GateItemDefId, string Type, string? Field, string? OldValue, string? NewValue,
    string? ByUser, DateTime AtUtc);
