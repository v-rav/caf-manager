namespace CafPortal.Application.Abstractions;

/// <summary>Builds Excel (.xlsx) workbooks for the main grids and an executive summary.</summary>
public interface IExportService
{
    Task<byte[]> ResourcesAsync(string? region, CancellationToken ct = default);
    Task<byte[]> CapacityAsync(string? region, CancellationToken ct = default);
    Task<byte[]> NominationsAsync(string? region, string? approval = null, string? migrationStatus = null,
        string? currentState = null, string? sla = null, string? links = null, string? search = null, int? fy = null,
        string? quality = null, int? stage = null, string? health = null, string? sa = null, string? pm = null, string? cftl = null,
        string? owner = null, CancellationToken ct = default);
    Task<byte[]> PerformanceAsync(string? region, CancellationToken ct = default);
    Task<byte[]> ExecutiveSummaryAsync(string? region, CancellationToken ct = default);
    Task<byte[]> AnalyticsAsync(string? region, string basis, string granularity, string measure,
        string? splitBy, int? fy, CancellationToken ct = default);
    Task<byte[]> ReconciliationAsync(string? region, CancellationToken ct = default);
}
