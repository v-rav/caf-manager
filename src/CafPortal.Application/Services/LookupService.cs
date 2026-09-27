using CafPortal.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class LookupService(IApplicationDbContext db) : ILookupService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<IReadOnlyList<string>> ValuesAsync(string category, CancellationToken ct = default)
        => await _db.LookupValues.AsNoTracking()
            .Where(l => l.Category == category && l.ActiveFlag)
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Value)
            .Select(l => l.Value)
            .ToListAsync(ct);
}
