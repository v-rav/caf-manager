using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Import;

/// <summary>
/// Normalizes raw account names to a canonical account using the AccountAlias table,
/// and upserts the Account row. Keeps an in-memory cache for a single import batch.
/// </summary>
internal sealed class AccountResolver(IApplicationDbContext db)
{
    private readonly IApplicationDbContext _db = db;
    private Dictionary<string, string>? _aliasMap;
    private readonly Dictionary<string, Account> _cache = new(StringComparer.OrdinalIgnoreCase);

    private async Task EnsureAliasesAsync(CancellationToken ct)
    {
        _aliasMap ??= await _db.AccountAliases.AsNoTracking()
            .ToDictionaryAsync(a => a.Alias, a => a.StandardAccountName,
                StringComparer.OrdinalIgnoreCase, ct);
    }

    public string Canonicalize(string rawName)
    {
        var trimmed = rawName.Trim();
        return _aliasMap is not null && _aliasMap.TryGetValue(trimmed, out var canonical)
            ? canonical
            : trimmed;
    }

    /// <summary>Returns an existing or newly-tracked Account for the normalized name.</summary>
    public async Task<Account> ResolveAsync(string rawName, string region, CancellationToken ct)
    {
        await EnsureAliasesAsync(ct);
        var canonical = Canonicalize(rawName);

        if (_cache.TryGetValue(canonical, out var cached))
            return cached;

        var existing = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountName == canonical, ct);
        if (existing is null)
        {
            existing = new Account
            {
                AccountName = canonical,
                Region = string.IsNullOrWhiteSpace(region) ? "UNSPECIFIED" : region,
                Status = "Active"
            };
            _db.Accounts.Add(existing);
        }
        else if (!string.IsNullOrWhiteSpace(region) && existing.Region == "UNSPECIFIED")
        {
            existing.Region = region;
            existing.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        _cache[canonical] = existing;
        return existing;
    }
}
