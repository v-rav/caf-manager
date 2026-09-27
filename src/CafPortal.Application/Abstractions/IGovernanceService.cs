using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>Materializes a nomination's gated checklist from the template and applies SA updates.</summary>
public interface IGovernanceService
{
    Task<NominationGovernanceDto?> GetForNominationAsync(int nominationId, CancellationToken ct = default);
    Task<NominationGovernanceDto?> UpdateItemAsync(int nominationId, int itemDefId, UpdateGateItemRequest req, CancellationToken ct = default);
}
