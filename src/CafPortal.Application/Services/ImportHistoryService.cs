using System.Text.Json;
using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class ImportHistoryService(IApplicationDbContext db) : IImportHistoryService
{
    private readonly IApplicationDbContext _db = db;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<ImportRunDto>> GetRunsAsync(int take = 100, CancellationToken ct = default)
        => await _db.ImportRuns.AsNoTracking()
            .OrderByDescending(r => r.StartedUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(r => new ImportRunDto
            {
                Id = r.Id,
                StartedUtc = r.StartedUtc,
                CompletedUtc = r.CompletedUtc,
                Source = r.Source,
                FileName = r.FileName,
                Added = r.Added,
                Updated = r.Updated,
                Withdrawn = r.Withdrawn,
                Unchanged = r.Unchanged,
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ImportChangeDto>> GetChangesAsync(int runId, CancellationToken ct = default)
    {
        var rows = await _db.ImportChanges.AsNoTracking()
            .Where(c => c.ImportRunId == runId)
            .OrderBy(c => c.ChangeType).ThenBy(c => c.Label)
            .ToListAsync(ct);

        return rows.Select(c => new ImportChangeDto
        {
            Id = c.Id,
            EntityType = c.EntityType,
            ExternalKey = c.ExternalKey,
            Label = c.Label,
            ChangeType = c.ChangeType,
            Changes = string.IsNullOrWhiteSpace(c.ChangedFields)
                ? []
                : JsonSerializer.Deserialize<List<FieldChangeDto>>(c.ChangedFields, JsonOpts) ?? [],
        }).ToList();
    }
}
