namespace CafPortal.Application.Abstractions;

/// <summary>Builds Excel (.xlsx) workbooks for the main grids and an executive summary.</summary>
public interface IExportService
{
    Task<byte[]> ResourcesAsync(string? region, CancellationToken ct = default);
    Task<byte[]> CapacityAsync(string? region, CancellationToken ct = default);
    Task<byte[]> NominationsAsync(string? region, CancellationToken ct = default);
    Task<byte[]> PerformanceAsync(string? region, CancellationToken ct = default);
    Task<byte[]> ExecutiveSummaryAsync(string? region, CancellationToken ct = default);
}
