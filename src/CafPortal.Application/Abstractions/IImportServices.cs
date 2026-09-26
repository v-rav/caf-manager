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

/// <summary>Loads the account master list ("Nominations In-Flight": Segment, TPID, Customer Name, Account ID).</summary>
public interface IAccountMasterImportService
{
    Task<int> ImportAsync(Stream workbook, CancellationToken ct = default);
}

/// <summary>Orchestrates the nightly pipeline: load sources, transform, rebuild capacity facts.</summary>
public interface IDataRefreshService
{
    Task<DataRefreshResultDto> RefreshAsync(CancellationToken ct = default);

    /// <summary>Saves an uploaded source workbook (kind: nominations|resources|leave|engagement) then runs a refresh.</summary>
    Task<DataRefreshResultDto> UploadAndRefreshAsync(string kind, Stream content, string fileName, CancellationToken ct = default);

    /// <summary>Moves no-TPID accounts (non-canonical: departments/apps/abbreviations) out of the master into ParkedAccount. apply=false previews.</summary>
    Task<DataRefreshResultDto> ParkNoTpidAccountsAsync(bool apply, CancellationToken ct = default);

    /// <summary>Restores every parked account back into the master (re-creates the account, resource links, and nomination references).</summary>
    Task<DataRefreshResultDto> UnparkAllAccountsAsync(CancellationToken ct = default);

    /// <summary>Re-runs the one-time seed on demand (configuration + resource enrichments). Normally seeding runs only once.</summary>
    Task<DataRefreshResultDto> ReseedAsync(CancellationToken ct = default);

    /// <summary>Merges casing/punctuation-variant duplicate accounts into the TPID-bearing master (apply=false previews).</summary>
    Task<DataRefreshResultDto> MergeDuplicateAccountsAsync(bool apply, CancellationToken ct = default);

    /// <summary>Freshness + row counts for the header banner.</summary>
    Task<DataStatusDto> GetStatusAsync(CancellationToken ct = default);
}
