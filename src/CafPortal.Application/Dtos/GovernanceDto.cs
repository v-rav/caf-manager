namespace CafPortal.Application.Dtos;

public record GateItemDto(
    int ItemDefId, string Key, string Label, string Kind, string? SubStage, string ResponsibleRole,
    bool Mandatory, int Order, string Status, string? Owner, DateTime? CompletedUtc, string? Ref, string? Notes,
    string? UpdatedBy, DateTime? UpdatedUtc);

public record GateDto(
    string Key, string Name, string? ExitCriteria, int Order, int Weight, string Status, int PercentComplete,
    IReadOnlyList<GateItemDto> Items);

public record NominationGovernanceDto(
    int NominationId, string? Account, string? Tpid, string? Classification, string? CurrentGateKey,
    int CompliancePercent, IReadOnlyList<GateDto> Gates);

public record UpdateGateItemRequest(string Status, string? Owner, string? Ref, string? Notes);
