using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>Page-access lives as ApplicationSettings (PageAccess:&lt;key&gt; = csv of roles); missing keys use the defaults below.</summary>
public class AccessService(IApplicationDbContext db) : IAccessService
{
    private const string Prefix = "PageAccess:";
    private static readonly string[] All = { "Admin", "Lead", "Sa" };
    private static readonly string[] AdminLead = { "Admin", "Lead" };
    private static readonly string[] AdminOnly = { "Admin" };

    // Page catalog: key (route segment), label, default allowed roles.
    private static readonly (string Key, string Label, string[] Default)[] Catalog =
    {
        ("dashboard", "Executive Dashboard", All),
        ("analytics", "Migration Analytics", AdminLead),
        ("acr-recovery", "ACR Recovery", AdminLead),
        ("resources", "Resource Hub", All),
        ("accounts", "Account Hub", All),
        ("capacity", "Capacity", All),
        ("reconciliation", "Reconciliation", AdminLead),
        ("leave", "Leave", All),
        ("nominations", "Nominations", All),
        ("governance", "Governance Board", AdminLead),
        ("strategic", "Strategic Register", AdminLead),
        ("adoption", "GHCP Adoption", AdminLead),
        ("flow", "Migration Flow", AdminLead),
        ("performance", "Performance", AdminLead),
        ("history", "Import History", AdminLead),
        ("help", "Help & FAQ", All),
        ("configuration", "Configuration", AdminOnly),
        ("gates", "Gate Template", AdminOnly),
        ("capability", "Capability Masters", AdminOnly),
        ("backup", "Backup & Restore", AdminOnly),
        ("access", "User & Access", AdminOnly),
    };

    public async Task<IReadOnlyList<PageAccessDto>> GetPagesAsync(CancellationToken ct = default)
    {
        var stored = await db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key.StartsWith(Prefix))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        return Catalog.Select(p =>
        {
            var roles = stored.TryGetValue(Prefix + p.Key, out var v) && !string.IsNullOrWhiteSpace(v)
                ? v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : p.Default;
            // Admin is always allowed.
            var set = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase) { "Admin" };
            return new PageAccessDto(p.Key, p.Label, set.OrderBy(r => r).ToList());
        }).ToList();
    }

    public async Task<IReadOnlyList<PageAccessDto>> SaveAsync(IReadOnlyList<PageAccessUpdate> updates, CancellationToken ct = default)
    {
        var valid = Catalog.Select(c => c.Key).ToHashSet();
        var existing = await db.ApplicationSettings.Where(s => s.Key.StartsWith(Prefix)).ToDictionaryAsync(s => s.Key, ct);
        foreach (var u in updates.Where(u => valid.Contains(u.Key)))
        {
            // Admin is implicit; store the rest.
            var roles = (u.AllowedRoles ?? Array.Empty<string>())
                .Where(r => !string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
                .Distinct();
            var value = string.Join(",", roles);
            var key = Prefix + u.Key;
            if (existing.TryGetValue(key, out var row)) row.Value = value;
            else db.ApplicationSettings.Add(new ApplicationSetting { Key = key, Value = value, Description = "Role-based page access" });
        }
        await db.SaveChangesAsync(ct);
        return await GetPagesAsync(ct);
    }
}
