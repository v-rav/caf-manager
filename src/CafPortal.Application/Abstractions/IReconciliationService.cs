using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>
/// Read-only data-quality view over the resource→account mappings that feed capacity:
/// flags each link as Master / SuggestMerge / Orphan and Keep / Drop (in-flight or not).
/// </summary>
public interface IReconciliationService
{
    Task<ReconciliationReportDto> GetAsync(string? region, CancellationToken ct = default);

    /// <summary>
    /// Seed NominationResource assignments from the in-flight resource→account links (the "Keep" set).
    /// Idempotent; pass apply=false for a read-only preview. Role is derived from the resource's own role.
    /// </summary>
    Task<SeedAssignmentResultDto> SeedAssignmentsAsync(string? region, bool apply, CancellationToken ct = default);
}
