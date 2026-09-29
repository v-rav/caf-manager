using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>Materializes a nomination's gated checklist from the template and applies SA updates.</summary>
public interface IGovernanceService
{
    Task<NominationGovernanceDto?> GetForNominationAsync(int nominationId, CancellationToken ct = default);
    Task<NominationGovernanceDto?> UpdateItemAsync(int nominationId, int itemDefId, UpdateGateItemRequest req, CancellationToken ct = default);
    Task<NominationGovernanceDto?> RaiseBlockerAsync(int nominationId, RaiseBlockerRequest req, CancellationToken ct = default);
    Task<NominationGovernanceDto?> ResolveBlockerAsync(int nominationId, int blockerId, CancellationToken ct = default);
    Task<IReadOnlyList<BlockerDto>> GetOpenBlockersAsync(string? region, CancellationToken ct = default);
    Task<IReadOnlyList<NominationEventDto>> GetEventsAsync(int nominationId, CancellationToken ct = default);
    Task<NominationGovernanceDto?> AddMilestoneAsync(int nominationId, MilestoneUpsert req, CancellationToken ct = default);
    Task<NominationGovernanceDto?> DeleteMilestoneAsync(int nominationId, int milestoneId, CancellationToken ct = default);
    Task<NominationGovernanceDto?> AddToolUsageAsync(int nominationId, ToolUsageUpsert req, CancellationToken ct = default);
    Task<NominationGovernanceDto?> DeleteToolUsageAsync(int nominationId, int usageId, CancellationToken ct = default);
    Task<bool> ApplyAcrCaptureAsync(int nominationId, AcrApplyRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<MigrationToolDto>> GetMigrationToolsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MigrationActivityDto>> GetMigrationActivitiesAsync(CancellationToken ct = default);
    Task<CapabilityUtilizationDto> GetCapabilityAsync(string? region, CancellationToken ct = default);
    IReadOnlyList<string> GetBlockerCategories();
}
