namespace CafPortal.Application.Abstractions;

/// <summary>Reads editable governance vocabularies (blocker categories/owners, classifications, velocity impacts).</summary>
public interface ILookupService
{
    Task<IReadOnlyList<string>> ValuesAsync(string category, CancellationToken ct = default);
}
