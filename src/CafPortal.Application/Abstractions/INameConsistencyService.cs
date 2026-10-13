using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>
/// Deterministic fuzzy name-consistency assist: clusters likely-same account/resource names and
/// applies approved variants as aliases. Review-first — suggestions never auto-apply.
/// </summary>
public interface INameConsistencyService
{
    /// <summary>Clusters account names (diacritics/suffix/word-order/typo/abbreviation variants). Read-only.</summary>
    Task<NameConsistencyReportDto> GetAccountSuggestionsAsync(double threshold, CancellationToken ct = default);

    /// <summary>Clusters roster resource names into near-duplicate people. Read-only.</summary>
    Task<NameConsistencyReportDto> GetResourceSuggestionsAsync(double threshold, CancellationToken ct = default);

    /// <summary>Registers the given variant names as aliases of the canonical account (AccountAlias + Account.Aliases).</summary>
    Task<NameConsistencyApplyResultDto> ApplyAccountAliasesAsync(ApplyAccountAliasRequest request, CancellationToken ct = default);

    /// <summary>Appends the given variant names onto the canonical resource's alias list.</summary>
    Task<NameConsistencyApplyResultDto> ApplyResourceAliasesAsync(ApplyResourceAliasRequest request, CancellationToken ct = default);

    /// <summary>Matches free-text performance-review names against the roster and suggests the canonical spelling. Read-only.</summary>
    Task<PerformanceNameReportDto> GetPerformanceSuggestionsAsync(double threshold, CancellationToken ct = default);

    /// <summary>Renames every performance review carrying CurrentName to the roster RosterName.</summary>
    Task<NameConsistencyApplyResultDto> ApplyPerformanceNameAsync(ApplyPerformanceNameRequest request, CancellationToken ct = default);

    /// <summary>Matches free-text FDO ownership names (SA/PM/CFTL) against the roster; flags gaps with candidate suggestions. Read-only.</summary>
    Task<OwnershipReportDto> GetOwnershipSuggestionsAsync(double threshold, CancellationToken ct = default);

    /// <summary>Renames a roster resource, preserving the old name as an alias.</summary>
    Task<NameConsistencyApplyResultDto> RenameResourceAsync(RenameResourceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Auto-resolves each Approved+in-flight nomination's SA to a roster resource (exact / alias / high-confidence
    /// fuzzy), learns the alias, and rebuilds the SA-derived assignment links so capacity follows the SA.
    /// Idempotent and reassignment-safe. apply=false previews. (SA only; CFTL/PM excluded for now.)
    /// </summary>
    Task<OwnershipLinkResultDto> LinkSolutionArchitectsAsync(bool apply, CancellationToken ct = default);
}
