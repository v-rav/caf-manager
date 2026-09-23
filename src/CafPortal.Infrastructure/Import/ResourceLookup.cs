using CafPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Import;

/// <summary>Resolves a resource id from PSID, email, or name during an import batch.</summary>
internal sealed class ResourceLookup
{
    private readonly Dictionary<string, int> _byPsid = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _byEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _byName = new(StringComparer.OrdinalIgnoreCase);

    private ResourceLookup() { }

    public static async Task<ResourceLookup> BuildAsync(AppDbContext db, CancellationToken ct)
    {
        var lookup = new ResourceLookup();
        var resources = await db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Psid, r.Email, r.Name })
            .ToListAsync(ct);

        foreach (var r in resources)
        {
            if (!string.IsNullOrWhiteSpace(r.Psid))
                lookup._byPsid[r.Psid] = r.ResourceId;
            if (!string.IsNullOrWhiteSpace(r.Email))
                lookup._byEmail[r.Email] = r.ResourceId;
            if (!string.IsNullOrWhiteSpace(r.Name))
                lookup._byName[r.Name] = r.ResourceId;
        }
        return lookup;
    }

    public int? Resolve(string? psid, string? email, string? name)
    {
        if (!string.IsNullOrWhiteSpace(psid) && _byPsid.TryGetValue(psid, out var id))
            return id;
        if (!string.IsNullOrWhiteSpace(email) && _byEmail.TryGetValue(email, out id))
            return id;
        if (!string.IsNullOrWhiteSpace(name) && _byName.TryGetValue(name, out id))
            return id;
        return null;
    }
}
