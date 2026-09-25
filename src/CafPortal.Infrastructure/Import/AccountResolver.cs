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
    // Normalized (case/punctuation-insensitive) index of existing accounts, preferring the master (TPID-bearing).
    private Dictionary<string, Account>? _byNorm;

    private async Task EnsureAliasesAsync(CancellationToken ct)
    {
        _aliasMap ??= await _db.AccountAliases.AsNoTracking()
            .ToDictionaryAsync(a => a.Alias, a => a.StandardAccountName,
                StringComparer.OrdinalIgnoreCase, ct);
    }

    private async Task EnsureIndexAsync(CancellationToken ct)
    {
        if (_byNorm is not null)
            return;
        _byNorm = new Dictionary<string, Account>();
        foreach (var a in await _db.Accounts.ToListAsync(ct))
            IndexAccount(a);
    }

    private void IndexAccount(Account a)
    {
        void Put(string? nm)
        {
            var k = Norm(nm);
            if (k.Length == 0)
                return;
            // When two accounts share a normalized key, keep the one with a TPID (the master).
            if (_byNorm!.TryGetValue(k, out var cur))
            {
                if (string.IsNullOrWhiteSpace(cur.Tpid) && !string.IsNullOrWhiteSpace(a.Tpid))
                    _byNorm[k] = a;
            }
            else
            {
                _byNorm![k] = a;
            }
        }
        Put(a.AccountName);
        if (!string.IsNullOrWhiteSpace(a.Aliases))
            foreach (var al in a.Aliases.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                Put(al);
    }

    private static string Norm(string? s) =>
        new string((s ?? string.Empty).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

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
        await EnsureIndexAsync(ct);
        var canonical = Canonicalize(rawName);

        if (_cache.TryGetValue(canonical, out var cached))
            return cached;

        // Exact canonical name first, then a normalized (case/punctuation-insensitive) match to the master —
        // avoids creating a duplicate account like "CVS Health" alongside the master "CVS HEALTH".
        var norm = Norm(canonical);
        var existing = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountName == canonical, ct);
        if (existing is null && norm.Length > 0 && _byNorm!.TryGetValue(norm, out var hit))
            existing = hit;

        if (existing is null)
        {
            existing = new Account
            {
                AccountName = canonical,
                Region = string.IsNullOrWhiteSpace(region) ? "UNSPECIFIED" : region,
                Status = "Active"
            };
            _db.Accounts.Add(existing);
            IndexAccount(existing);
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
