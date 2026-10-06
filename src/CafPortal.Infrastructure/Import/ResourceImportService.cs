using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CafPortal.Infrastructure.Import;

/// <summary>
/// Imports resources and account mappings from an Excel workbook.
/// Recognizes the APP-TEAM two-sheet layout (a "Resource" master sheet with PSID/experience and a
/// "Resource-Accounts" mapping sheet with Primary/Secondary account lists); otherwise falls back to a
/// generic single-sheet import. Column names are discovered from headers so layout drift is tolerated.
/// </summary>
public class ResourceImportService(AppDbContext db, ILogger<ResourceImportService> logger) : IResourceImportService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<ResourceImportService> _logger = logger;
    private HashSet<string>? _knownRegions;

    public async Task<int> ImportAsync(Stream workbook, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook(workbook);
        var sheets = wb.Worksheets.ToList();

        var masterWs = sheets.FirstOrDefault(w => SheetHasAnyHeader(w, "PS ID", "PSID", "LTM PS", "Years Of Experience"));
        var mappingWs = sheets.FirstOrDefault(w =>
            SheetHasAnyHeader(w, "Primary Account", "Secondary Account") ||
            w.Name.Contains("Account", StringComparison.OrdinalIgnoreCase));

        // Neither recognizable -> generic single-sheet import.
        if (masterWs is null && mappingWs is null)
            return await ImportGenericAsync(sheets.First(), ct);

        var resolver = new AccountResolver(_db);
        var masterCount = masterWs is not null ? await ImportMasterAsync(masterWs, ct) : 0;
        var mappingCount = mappingWs is not null ? await ImportMappingAsync(mappingWs, resolver, ct) : 0;

        var touched = masterCount > 0 ? masterCount : mappingCount;
        _logger.LogInformation("ResourceImportService: master={Master}, mapping={Mapping}", masterCount, mappingCount);
        return touched;
    }

    // --- APP-TEAM master sheet: one row per resource (PSID, experience, skill, region, role) ---
    private async Task<int> ImportMasterAsync(IXLWorksheet ws, CancellationToken ct)
    {
        var rows = ws.RowsUsed().ToList();
        if (rows.Count < 2)
            return 0;

        var h = ExcelHelpers.MapHeaders(rows[0]);
        var colName = ExcelHelpers.FindColumn(h, "Name", "Resource");
        var colPsid = ExcelHelpers.FindColumn(h, "PS ID", "PSID", "LTM PS", "EmployeeId", "Emp Id");
        var colEmail = ExcelHelpers.FindColumn(h, "Email", "Mail");
        var colExp = ExcelHelpers.FindColumn(h, "Years Of Experience", "Experience", "Exp");
        var colSkill = ExcelHelpers.FindColumn(h, "Primary Skill", "Skill", "Technology");
        var colStatus = ExcelHelpers.FindColumn(h, "Status");
        var colRegion = ExcelHelpers.FindColumn(h, "Region", "Geo");
        var colRole = ExcelHelpers.FindColumn(h, "Role", "Designation");
        var colSeparated = ExcelHelpers.FindColumn(h, "Separated");
        var colGrade = ExcelHelpers.FindColumn(h, "Grade");
        var colMobile = ExcelHelpers.FindColumn(h, "Mobile", "Phone");

        var count = 0;
        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;

            var name = Clean(ExcelHelpers.GetString(row, colName));
            var psid = Clean(ExcelHelpers.GetString(row, colPsid));
            var email = Clean(ExcelHelpers.GetString(row, colEmail));
            if (name is null && psid is null)
                continue;

            var region = NormalizeRegion(ExcelHelpers.GetString(row, colRegion));
            var resource = await FindOrCreateResourceAsync(psid, email, name, ct);
            resource.Name = name ?? resource.Name;
            resource.Psid = psid ?? resource.Psid;
            resource.Email = email ?? resource.Email;
            // Don't clobber a known region with UNSPECIFIED from a blank cell (preserves portal edits).
            if (region != "UNSPECIFIED" || string.IsNullOrWhiteSpace(resource.Region))
                resource.Region = region;
            resource.Role = ExcelHelpers.GetString(row, colRole) ?? resource.Role;
            resource.Grade = Clean(ExcelHelpers.GetString(row, colGrade)) ?? resource.Grade;
            resource.Mobile = Clean(ExcelHelpers.GetString(row, colMobile)) ?? resource.Mobile;
            var skill = ExcelHelpers.GetString(row, colSkill);
            resource.PrimarySkill = skill ?? resource.PrimarySkill;
            resource.Skills = skill ?? resource.Skills;
            resource.ExperienceYears = ParseExperience(ExcelHelpers.GetString(row, colExp), resource.ExperienceYears);
            resource.Status = Clean(ExcelHelpers.GetString(row, colStatus)) ?? resource.Status ?? "Active";
            resource.Separated = ParseFlag(ExcelHelpers.GetString(row, colSeparated), resource.Separated);
            resource.ActiveFlag = !resource.Separated;
            if (resource.CapacityLimit <= 0)
                resource.CapacityLimit = 5;
            resource.UpdatedUtc = DateTimeOffset.UtcNow;

            await EnsureRegionAsync(region, ct);
            count++;
        }

        await _db.SaveChangesAsync(ct);
        return count;
    }

    // --- APP-TEAM mapping sheet: Name + Primary/Secondary account lists ---
    private async Task<int> ImportMappingAsync(IXLWorksheet ws, AccountResolver resolver, CancellationToken ct)
    {
        var rows = ws.RowsUsed().ToList();
        if (rows.Count < 2)
            return 0;

        var h = ExcelHelpers.MapHeaders(rows[0]);
        var colName = ExcelHelpers.FindColumn(h, "Name", "Resource");
        var colRegion = ExcelHelpers.FindColumn(h, "Region", "Geo");
        var colRole = ExcelHelpers.FindColumn(h, "Role", "Designation");
        var colSeparated = ExcelHelpers.FindColumn(h, "Separated");
        var colPrimary = ExcelHelpers.FindColumn(h, "Primary Account", "Primary Accounts");
        var colSecondary = ExcelHelpers.FindColumn(h, "Secondary Account", "Secondary Accounts");
        // Single-column fallback if this sheet uses one combined account column.
        var colAccounts = colPrimary is null && colSecondary is null
            ? ExcelHelpers.FindColumn(h, "Account", "Customer", "Mapping")
            : null;

        var count = 0;
        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;
            var name = Clean(ExcelHelpers.GetString(row, colName));
            if (name is null)
                continue;

            var region = NormalizeRegion(ExcelHelpers.GetString(row, colRegion));
            var resource = await FindOrCreateResourceAsync(null, null, name, ct);
            resource.Name = name;
            if (string.IsNullOrWhiteSpace(resource.Region) || resource.Region == "UNSPECIFIED")
                resource.Region = region;
            resource.Role = ExcelHelpers.GetString(row, colRole) ?? resource.Role;
            resource.Separated = ParseFlag(ExcelHelpers.GetString(row, colSeparated), resource.Separated);
            if (resource.CapacityLimit <= 0)
                resource.CapacityLimit = 5;
            resource.ActiveFlag = !resource.Separated;
            await EnsureRegionAsync(region, ct);
            await _db.SaveChangesAsync(ct); // materialize resource id

            await LinkAccountsAsync(resource, ExcelHelpers.GetString(row, colPrimary), "Primary", region, resolver, ct);
            await LinkAccountsAsync(resource, ExcelHelpers.GetString(row, colSecondary), "Secondary", region, resolver, ct);
            await LinkAccountsAsync(resource, ExcelHelpers.GetString(row, colAccounts), "Primary", region, resolver, ct);
            count++;
        }

        return count;
    }

    private async Task LinkAccountsAsync(Resource resource, string? cell, string relationship, string region,
        AccountResolver resolver, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cell))
            return;
        foreach (var raw in SplitAccounts(cell))
        {
            var name = CleanAccountName(raw);
            if (name is null)
                continue;
            var account = await resolver.ResolveAsync(name, region, ct);
            await _db.SaveChangesAsync(ct); // ensure account id
            var exists = await _db.ResourceAccounts.AnyAsync(
                x => x.ResourceId == resource.ResourceId && x.AccountId == account.AccountId, ct);
            if (!exists)
            {
                _db.ResourceAccounts.Add(new ResourceAccount
                {
                    ResourceId = resource.ResourceId,
                    AccountId = account.AccountId,
                    Source = "ResourceMapping",
                    RelationshipType = relationship
                });
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    // --- Generic single-sheet fallback (original behavior) ---
    private async Task<int> ImportGenericAsync(IXLWorksheet ws, CancellationToken ct)
    {
        var rows = ws.RowsUsed().ToList();
        if (rows.Count < 2)
            return 0;

        var headers = ExcelHelpers.MapHeaders(rows[0]);
        var colName = ExcelHelpers.FindColumn(headers, "Name", "Resource");
        var colPsid = ExcelHelpers.FindColumn(headers, "PSID", "PS ID", "EmployeeId", "Emp Id");
        var colEmail = ExcelHelpers.FindColumn(headers, "Email", "Mail");
        var colRegion = ExcelHelpers.FindColumn(headers, "Region", "Geo");
        var colRole = ExcelHelpers.FindColumn(headers, "Role", "Designation");
        var colPrimarySkill = ExcelHelpers.FindColumn(headers, "Primary Skill", "PrimarySkill", "Primary Technology");
        var colSkills = ExcelHelpers.FindColumn(headers, "Skills", "Secondary Skill", "Technology");
        var colExp = ExcelHelpers.FindColumn(headers, "Experience", "Exp");
        var colStatus = ExcelHelpers.FindColumn(headers, "Status");
        var colSeparated = ExcelHelpers.FindColumn(headers, "Separated");
        var colAccounts = ExcelHelpers.FindColumn(headers, "Account", "Customer", "Mapping");
        var colRelationship = ExcelHelpers.FindColumn(headers, "Primary/Secondary", "Relationship", "Primary Secondary");

        var resolver = new AccountResolver(_db);
        var imported = 0;
        foreach (var row in rows.Skip(1))
        {
            if (ExcelHelpers.IsEmptyRow(row))
                continue;
            var name = Clean(ExcelHelpers.GetString(row, colName));
            var psid = Clean(ExcelHelpers.GetString(row, colPsid));
            var email = Clean(ExcelHelpers.GetString(row, colEmail));
            if (name is null && psid is null && email is null)
                continue;

            var region = NormalizeRegion(ExcelHelpers.GetString(row, colRegion));
            var resource = await FindOrCreateResourceAsync(psid, email, name, ct);
            resource.Name = name ?? resource.Name;
            resource.Psid = psid ?? resource.Psid;
            resource.Email = email ?? resource.Email;
            // Don't clobber a known region with UNSPECIFIED from a blank cell (preserves portal edits).
            if (region != "UNSPECIFIED" || string.IsNullOrWhiteSpace(resource.Region))
                resource.Region = region;
            resource.Role = ExcelHelpers.GetString(row, colRole) ?? resource.Role;
            resource.PrimarySkill = ExcelHelpers.GetString(row, colPrimarySkill) ?? resource.PrimarySkill;
            resource.Skills = ExcelHelpers.GetString(row, colSkills) ?? resource.Skills;
            resource.ExperienceYears = ParseExperience(ExcelHelpers.GetString(row, colExp), resource.ExperienceYears);
            resource.Status = Clean(ExcelHelpers.GetString(row, colStatus)) ?? resource.Status ?? "Active";
            resource.Separated = ParseFlag(ExcelHelpers.GetString(row, colSeparated), resource.Separated);
            resource.ActiveFlag = !resource.Separated;
            if (resource.CapacityLimit <= 0)
                resource.CapacityLimit = 5;
            resource.UpdatedUtc = DateTimeOffset.UtcNow;
            await EnsureRegionAsync(region, ct);
            await _db.SaveChangesAsync(ct);

            var relationship = ExcelHelpers.GetString(row, colRelationship) ?? "Primary";
            await LinkAccountsAsync(resource, ExcelHelpers.GetString(row, colAccounts), relationship, region, resolver, ct);
            imported++;
        }

        _logger.LogInformation("ResourceImportService (generic) imported {Count} resources", imported);
        return imported;
    }

    private async Task<Resource> FindOrCreateResourceAsync(string? psid, string? email, string? name, CancellationToken ct)
    {
        // Check the change tracker first so rows added earlier in this batch (not yet saved) are reused.
        Resource? resource =
            (!string.IsNullOrWhiteSpace(psid) ? _db.Resources.Local.FirstOrDefault(r => r.Psid == psid) : null)
            ?? (!string.IsNullOrWhiteSpace(email) ? _db.Resources.Local.FirstOrDefault(r => r.Email == email) : null)
            ?? (!string.IsNullOrWhiteSpace(name)
                ? _db.Resources.Local.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase))
                : null);

        if (resource is null && !string.IsNullOrWhiteSpace(psid))
            resource = await _db.Resources.FirstOrDefaultAsync(r => r.Psid == psid, ct);
        if (resource is null && !string.IsNullOrWhiteSpace(email))
            resource = await _db.Resources.FirstOrDefaultAsync(r => r.Email == email, ct);
        if (resource is null && !string.IsNullOrWhiteSpace(name))
            resource = await _db.Resources.FirstOrDefaultAsync(r => r.Name == name, ct);
        // Alias-aware, case/punctuation-insensitive fallback so a name variant matches an
        // existing record (via Name or Aliases) instead of creating a duplicate on every refresh.
        if (resource is null && !string.IsNullOrWhiteSpace(name))
            resource = await FindByNormalizedNameOrAliasAsync(name, ct);

        if (resource is null)
        {
            resource = new Resource { Name = name ?? psid ?? email ?? "Unknown", CapacityLimit = 5 };
            _db.Resources.Add(resource);
        }
        return resource;
    }

    private static string NormName(string? s) => new string((s ?? string.Empty).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static bool MatchesNameOrAlias(Resource r, string norm)
    {
        if (NormName(r.Name) == norm)
            return true;
        return !string.IsNullOrWhiteSpace(r.Aliases)
            && r.Aliases.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(a => NormName(a) == norm);
    }

    private async Task<Resource?> FindByNormalizedNameOrAliasAsync(string name, CancellationToken ct)
    {
        var norm = NormName(name);
        if (norm.Length == 0)
            return null;
        var local = _db.Resources.Local.FirstOrDefault(r => MatchesNameOrAlias(r, norm));
        if (local is not null)
            return local;
        var all = await _db.Resources.ToListAsync(ct);
        return all.FirstOrDefault(r => MatchesNameOrAlias(r, norm));
    }

    private async Task EnsureRegionAsync(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || code == "UNSPECIFIED")
            return;

        _knownRegions ??= (await _db.Regions.Select(r => r.Code).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_knownRegions.Add(code))
            _db.Regions.Add(new RegionConfiguration { Code = code, DisplayName = code, SortOrder = 99, ActiveFlag = true });
    }

    private static bool SheetHasAnyHeader(IXLWorksheet ws, params string[] candidates)
    {
        var header = ws.RowsUsed().FirstOrDefault();
        if (header is null)
            return false;
        var map = ExcelHelpers.MapHeaders(header);
        return ExcelHelpers.FindColumn(map, candidates) is not null;
    }

    private static IEnumerable<string> SplitAccounts(string cell) => cell
        .Split([',', ';', '\n', '\r', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>Strips a trailing "(REGION)" suffix and drops NA/None placeholders.</summary>
    private static string? CleanAccountName(string raw)
    {
        var name = raw;
        var paren = name.IndexOf('(');
        if (paren > 0)
            name = name[..paren];
        name = Clean(name) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return null;
        return name.Equals("NA", StringComparison.OrdinalIgnoreCase)
            || name.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            || name.Equals("None", StringComparison.OrdinalIgnoreCase)
                ? null
                : name;
    }

    private static string NormalizeRegion(string? raw)
    {
        var value = Clean(raw);
        if (string.IsNullOrWhiteSpace(value))
            return "UNSPECIFIED";
        // Take the primary region when the cell lists several (e.g. "EMEA, ASIA" -> "EMEA").
        var primary = value.Split([',', '/', '&'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? value;
        return primary.ToUpperInvariant() switch
        {
            "US" or "USA" or "AMER" or "AMERICAS" or "NA" or "NORTH AMERICA" => "AMER",
            "EMEA" => "EMEA",
            "ASIA" or "APAC" => "ASIA",
            "ALL" or "GLOBAL" => "ALL",
            _ => primary.ToUpperInvariant()
        };
    }

    /// <summary>Parses values like "~18", "8", "12+" leniently; blank keeps the fallback.</summary>
    private static double ParseExperience(string? value, double fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        var digits = new string(value.Where(c => char.IsDigit(c) || c == '.').ToArray());
        return double.TryParse(digits, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    private static bool ParseFlag(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        return value.Trim().ToLowerInvariant() is "y" or "yes" or "true" or "1" or "dedicated";
    }

    /// <summary>Trims and removes zero-width / non-breaking whitespace that Excel exports sometimes carry.</summary>
    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var cleaned = value.Replace("\u200b", string.Empty).Replace("\u00a0", " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }
}
