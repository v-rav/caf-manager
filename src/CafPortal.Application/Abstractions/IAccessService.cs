namespace CafPortal.Application.Abstractions;

using CafPortal.Application.Dtos;

/// <summary>Editable role-based page access. Admin always has access to every page.</summary>
public interface IAccessService
{
    Task<IReadOnlyList<PageAccessDto>> GetPagesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PageAccessDto>> SaveAsync(IReadOnlyList<PageAccessUpdate> updates, CancellationToken ct = default);
}
