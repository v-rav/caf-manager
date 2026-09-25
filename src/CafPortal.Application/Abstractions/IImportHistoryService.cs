using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>Reads the import audit trail (what each FDO upload / refresh changed) for the History page.</summary>
public interface IImportHistoryService
{
    Task<IReadOnlyList<ImportRunDto>> GetRunsAsync(int take = 100, CancellationToken ct = default);
    Task<IReadOnlyList<ImportChangeDto>> GetChangesAsync(int runId, CancellationToken ct = default);
}
