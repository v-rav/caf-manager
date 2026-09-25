using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>Creates a compressed snapshot of the SQLite database and restores from one.</summary>
public interface IBackupService
{
    /// <summary>Consistent snapshot of the live DB, zipped in memory. Returns the bytes and a timestamped file name.</summary>
    Task<(byte[] Content, string FileName)> CreateBackupAsync(CancellationToken ct = default);

    /// <summary>Replaces the live DB with the one inside the uploaded zip after validating it.</summary>
    Task<RestoreResultDto> RestoreAsync(Stream zipStream, CancellationToken ct = default);
}
