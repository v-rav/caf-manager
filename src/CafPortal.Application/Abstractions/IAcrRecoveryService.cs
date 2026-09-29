using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>
/// ACR recovery ledger: flag a nomination for under-captured ACR (freezing a baseline), track SA
/// notification, and reconcile against later imports to book realized recovery attributable to the analysis.
/// </summary>
public interface IAcrRecoveryService
{
    Task<IReadOnlyList<AcrRecoveryDto>> GetAllAsync(string? region, CancellationToken ct = default);
    Task<AcrRecoverySummaryDto> GetSummaryAsync(string? region, CancellationToken ct = default);
    Task<bool> FlagAsync(AcrRecoveryFlagRequest req, CancellationToken ct = default);
    Task<bool> MarkNotifiedAsync(int id, CancellationToken ct = default);
    Task<bool> CloseAsync(int id, string? note, CancellationToken ct = default);
    /// <summary>Books realized recovery for open claims whose nomination ACR has risen above baseline. Returns count realized.</summary>
    Task<int> ReconcileAsync(CancellationToken ct = default);
}
