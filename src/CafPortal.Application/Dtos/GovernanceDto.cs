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
    int CompliancePercent, IReadOnlyList<GateDto> Gates, IReadOnlyList<BlockerDto> Blockers);

public record BlockerDto(
    int Id, int NominationId, string? Account, int? GateItemDefId, string Category, bool ClockStopped, string? Owner,
    DateTime BlockedSinceUtc, DateTime? ExpectedResolutionUtc, DateTime? ResolvedUtc, string? Notes, int DaysBlocked,
    string? RaisedBy);

public record UpdateGateItemRequest(string Status, string? Owner, string? Ref, string? Notes);

public record RaiseBlockerRequest(string Category, bool ClockStopped, string? Owner, DateTime? ExpectedResolutionUtc, string? Notes, int? GateItemDefId);

public record NominationEventDto(
    int Id, int NominationId, int? GateItemDefId, string Type, string? Field, string? OldValue, string? NewValue,
    string? ByUser, DateTime AtUtc);
