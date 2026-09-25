using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>Loads a single source workbook and returns the number of rows ingested.</summary>
public interface IResourceImportService
{
    Task<int> ImportAsync(Stream workbook, CancellationToken ct = default);
}

public interface ILeaveImportService
{
    Task<int> ImportAsync(Stream workbook, CancellationToken ct = default);
}

public interface IEngagementImportService
{
    Task<int> ImportAsync(Stream workbook, CancellationToken ct = default);
}

/// <summary>Loads the nomination pipeline export ("Detail View"), upserting accounts and nominations.</summary>
public interface INominationImportService
{
    Task<int> ImportAsync(Stream workbook, CancellationToken ct = default);
}

/// <summary>Orchestrates the nightly pipeline: load sources, transform, rebuild capacity facts.</summary>
public interface IDataRefreshService
{
    Task<DataRefreshResultDto> RefreshAsync(CancellationToken ct = default);

    /// <summary>Freshness + row counts for the header banner.</summary>
    Task<DataStatusDto> GetStatusAsync(CancellationToken ct = default);
}
