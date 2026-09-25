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

    /// <summary>
    /// List people named in FDO ownership fields (SA/PM/CFTL) on in-flight waves who have no matching
    /// portal resource (by name or alias) — candidates to add to the roster. Read-only.
    /// </summary>
    Task<UnmatchedPeopleResultDto> GetUnmatchedPeopleAsync(string? region, CancellationToken ct = default);

    /// <summary>
    /// Repoint near-duplicate resource→account links (SuggestMerge) onto their master account,
    /// deduping where the master is already linked. Idempotent; pass apply=false for a preview.
    /// </summary>
    Task<LinkCleanupResultDto> CleanupLinksAsync(string? region, bool apply, CancellationToken ct = default);
}
