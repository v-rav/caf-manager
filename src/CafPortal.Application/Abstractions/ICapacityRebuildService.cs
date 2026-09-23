namespace CafPortal.Application.Abstractions;

/// <summary>Rebuilds the materialized CapacityFact snapshot after data changes.</summary>
public interface ICapacityRebuildService
{
    /// <summary>Recomputes capacity for every active resource. Returns the number of rows written.</summary>
    Task<int> RebuildAllAsync(CancellationToken ct = default);

    /// <summary>Recomputes capacity for a single resource (after a mapping or resource change).</summary>
    Task RebuildForResourceAsync(int resourceId, CancellationToken ct = default);
}
