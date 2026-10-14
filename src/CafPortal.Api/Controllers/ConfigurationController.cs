using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Api.Controllers;

/// <summary>Configuration metadata that drives the UI without code changes. Read: any signed-in user; edits: Admin.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ConfigurationController(
    IApplicationDbContext db,
    ICapacityCalculationService capacity,
    ICapacityRebuildService rebuild) : ControllerBase
{
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions(CancellationToken ct)
        => Ok(await db.Regions.AsNoTracking()
            .Where(r => r.ActiveFlag)
            .OrderBy(r => r.SortOrder)
            .Select(r => new { r.Code, r.DisplayName })
            .ToListAsync(ct));

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
        => Ok(await db.Roles.AsNoTracking()
            .Where(r => r.ActiveFlag)
            .OrderBy(r => r.SortOrder)
            .Select(r => new { r.RoleName, r.Description })
            .ToListAsync(ct));

    /// <summary>Optimal active-account capacity each role type can handle.</summary>
    [HttpGet("capacity")]
    public async Task<IActionResult> GetCapacity(CancellationToken ct)
        => Ok(await BuildCapacityAsync(ct));
    [HttpPut("capacity")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCapacity([FromBody] IReadOnlyList<RoleCapacityUpdate> updates, CancellationToken ct)
    {
        if (updates is null || updates.Count == 0)
            return BadRequest("No capacity updates supplied.");
        if (updates.Any(u => u.CapacityLimit < 1))
            return BadRequest("Capacity limit must be at least 1.");

        var configs = await db.CapacityConfigurations.ToListAsync(ct);
        foreach (var u in updates)
        {
            var role = u.RoleName?.Trim();
            if (string.IsNullOrEmpty(role)) continue;
            var existing = configs.FirstOrDefault(c => string.Equals(c.RoleName, role, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                db.CapacityConfigurations.Add(new CapacityConfiguration
                {
                    RoleName = role,
                    CapacityLimit = u.CapacityLimit,
                    WarningThreshold = 80,
                    OverloadedThreshold = 100,
                });
            }
            else
            {
                existing.CapacityLimit = u.CapacityLimit;
            }
        }

        await db.SaveChangesAsync(ct);
        await rebuild.RebuildAllAsync(ct);
        return Ok(await BuildCapacityAsync(ct));
    }

    private async Task<IReadOnlyList<RoleCapacityDto>> BuildCapacityAsync(CancellationToken ct)
    {
        var defaultLimit = await capacity.ResolveCapacityLimitAsync(string.Empty, null, ct);
        var configs = await db.CapacityConfigurations.AsNoTracking()
            .ToDictionaryAsync(c => c.RoleName, StringComparer.OrdinalIgnoreCase, ct);
        var roles = await db.Roles.AsNoTracking()
            .Where(r => r.ActiveFlag)
            .OrderBy(r => r.SortOrder)
            .Select(r => new { r.RoleName, r.Description })
            .ToListAsync(ct);

        return roles.Select(r =>
        {
            var configured = configs.TryGetValue(r.RoleName, out var c);
            return new RoleCapacityDto(
                r.RoleName,
                r.Description,
                configured ? c!.CapacityLimit : defaultLimit,
                configured);
        }).ToList();
    }

    public record RoleCapacityDto(string RoleName, string? Description, int CapacityLimit, bool Configured);
    public record RoleCapacityUpdate(string RoleName, int CapacityLimit);

    /// <summary>Account segments used across the portal.</summary>
    [HttpGet("segments")]
    public async Task<IActionResult> GetSegments(CancellationToken ct)
        => Ok(await db.Segments.AsNoTracking()
            .Where(s => s.ActiveFlag)
            .OrderBy(s => s.SortOrder)
            .Select(s => new { s.Id, s.Name, s.SortOrder })
            .ToListAsync(ct));

    /// <summary>Add a new account segment.</summary>
    [HttpPost("segments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddSegment([FromBody] SegmentUpsert input, CancellationToken ct)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Segment name is required.");
        if (await db.Segments.AnyAsync(s => s.Name == name, ct))
            return Conflict($"Segment '{name}' already exists.");

        var maxSort = await db.Segments.AnyAsync(ct) ? await db.Segments.MaxAsync(s => s.SortOrder, ct) : 0;
        var segment = new SegmentConfiguration { Name = name, SortOrder = maxSort + 1 };
        db.Segments.Add(segment);
        await db.SaveChangesAsync(ct);
        return Ok(new { segment.Id, segment.Name, segment.SortOrder });
    }

    /// <summary>Rename an existing account segment.</summary>
    [HttpPut("segments/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSegment(int id, [FromBody] SegmentUpsert input, CancellationToken ct)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Segment name is required.");
        var segment = await db.Segments.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (segment is null)
            return NotFound();
        if (await db.Segments.AnyAsync(s => s.Name == name && s.Id != id, ct))
            return Conflict($"Segment '{name}' already exists.");

        segment.Name = name;
        await db.SaveChangesAsync(ct);
        return Ok(new { segment.Id, segment.Name, segment.SortOrder });
    }

    public record SegmentUpsert(string Name);

    /// <summary>Controlled skill vocabulary.</summary>
    [HttpGet("skills")]
    public async Task<IActionResult> GetSkills(CancellationToken ct)
        => Ok(await db.Skills.AsNoTracking().Where(s => s.ActiveFlag)
            .OrderBy(s => s.SortOrder).Select(s => new { s.Id, s.Name, s.SortOrder }).ToListAsync(ct));

    [HttpPost("skills")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddSkill([FromBody] VocabUpsert input, CancellationToken ct)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Skill name is required.");
        if (await db.Skills.AnyAsync(s => s.Name == name, ct)) return Conflict($"Skill '{name}' already exists.");
        var maxSort = await db.Skills.AnyAsync(ct) ? await db.Skills.MaxAsync(s => s.SortOrder, ct) : 0;
        var skill = new SkillConfiguration { Name = name, SortOrder = maxSort + 1 };
        db.Skills.Add(skill);
        await db.SaveChangesAsync(ct);
        return Ok(new { skill.Id, skill.Name, skill.SortOrder });
    }

    [HttpPut("skills/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSkill(int id, [FromBody] VocabUpsert input, CancellationToken ct)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Skill name is required.");
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (skill is null) return NotFound();
        if (await db.Skills.AnyAsync(s => s.Name == name && s.Id != id, ct)) return Conflict($"Skill '{name}' already exists.");
        skill.Name = name;
        await db.SaveChangesAsync(ct);
        return Ok(new { skill.Id, skill.Name, skill.SortOrder });
    }

    public record VocabUpsert(string Name);

    /// <summary>Editable governance vocabularies (blocker categories/owners, classifications, velocity impacts).</summary>
    [HttpGet("lookups")]
    public async Task<IActionResult> GetLookups([FromQuery] string? category, CancellationToken ct)
    {
        var q = db.LookupValues.AsNoTracking().Where(l => l.ActiveFlag);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(l => l.Category == category);
        return Ok(await q.OrderBy(l => l.Category).ThenBy(l => l.SortOrder).ThenBy(l => l.Value)
            .Select(l => new { l.Id, l.Category, l.Value, l.SortOrder }).ToListAsync(ct));
    }

    [HttpPost("lookups")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddLookup([FromBody] LookupUpsert input, CancellationToken ct)
    {
        var category = input.Category?.Trim();
        var value = input.Value?.Trim();
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(value)) return BadRequest("Category and value are required.");
        if (await db.LookupValues.AnyAsync(l => l.Category == category && l.Value == value, ct)) return Conflict($"'{value}' already exists in {category}.");
        var maxSort = await db.LookupValues.Where(l => l.Category == category).AnyAsync(ct)
            ? await db.LookupValues.Where(l => l.Category == category).MaxAsync(l => l.SortOrder, ct) : 0;
        var lookup = new LookupValue { Category = category, Value = value, SortOrder = maxSort + 1 };
        db.LookupValues.Add(lookup);
        await db.SaveChangesAsync(ct);
        return Ok(new { lookup.Id, lookup.Category, lookup.Value, lookup.SortOrder });
    }

    [HttpPut("lookups/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateLookup(int id, [FromBody] VocabUpsert input, CancellationToken ct)
    {
        var value = input.Name?.Trim();
        if (string.IsNullOrEmpty(value)) return BadRequest("Value is required.");
        var lookup = await db.LookupValues.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lookup is null) return NotFound();
        if (await db.LookupValues.AnyAsync(l => l.Category == lookup.Category && l.Value == value && l.Id != id, ct))
            return Conflict($"'{value}' already exists in {lookup.Category}.");
        lookup.Value = value;
        await db.SaveChangesAsync(ct);
        return Ok(new { lookup.Id, lookup.Category, lookup.Value, lookup.SortOrder });
    }

    [HttpDelete("lookups/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLookup(int id, CancellationToken ct)
    {
        var lookup = await db.LookupValues.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lookup is null) return NotFound();
        db.LookupValues.Remove(lookup);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public record LookupUpsert(string Category, string Value);

    /// <summary>Editable operational settings, described by <see cref="SettingsCatalog"/> (label, group, bounds).</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var keys = SettingsCatalog.All.Select(d => d.Key).ToList();
        var stored = await db.ApplicationSettings.AsNoTracking()
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        return Ok(SettingsCatalog.All.Select(d => new
        {
            d.Key,
            Value = stored.TryGetValue(d.Key, out var v) ? v : d.Default,
            d.Description, d.Label, d.Group, d.Min, d.Max, d.Integer, d.Default, d.Section,
        }));
    }

    [HttpPut("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSettings([FromBody] IReadOnlyList<SettingUpsert> updates, CancellationToken ct)
    {
        if (updates is null || updates.Count == 0) return BadRequest("No settings supplied.");
        var keys = SettingsCatalog.All.Select(d => d.Key).ToList();
        var rows = await db.ApplicationSettings.Where(s => keys.Contains(s.Key)).ToListAsync(ct);

        // Validate the full candidate set (stored values merged with edits) so tier ordering is checked as a whole.
        var candidate = SettingsCatalog.All.ToDictionary(
            d => d.Key,
            d => double.TryParse(rows.FirstOrDefault(r => r.Key == d.Key)?.Value ?? d.Default,
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var cur)
                ? cur : double.Parse(d.Default, System.Globalization.CultureInfo.InvariantCulture));
        foreach (var u in updates)
        {
            if (SettingsCatalog.Find(u.Key) is null) return BadRequest($"Unknown setting '{u.Key}'.");
            if (!double.TryParse(u.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n))
                return BadRequest($"'{u.Value}' is not a number.");
            candidate[SettingsCatalog.Find(u.Key)!.Key] = n;
        }
        if (SettingsCatalog.Validate(candidate) is { } error) return BadRequest(error);

        var capacityChanged = false;
        foreach (var u in updates)
        {
            var def = SettingsCatalog.Find(u.Key)!;
            var value = candidate[def.Key].ToString(System.Globalization.CultureInfo.InvariantCulture);
            var row = rows.FirstOrDefault(r => r.Key == def.Key);
            if (row is null)
                db.ApplicationSettings.Add(new ApplicationSetting { Key = def.Key, Value = value, Description = def.Description });
            else if (row.Value != value)
                row.Value = value;
            else
                continue;
            if (def.Group == "Capacity") capacityChanged = true;
        }
        await db.SaveChangesAsync(ct);
        // Capacity status/limits are materialized in CapacityFact — rebuild so every page reflects the new thresholds.
        if (capacityChanged) await rebuild.RebuildAllAsync(ct);
        return await GetSettings(ct);
    }

    public record SettingUpsert(string Key, string Value);

    // ---- Fiscal-year ACR targets (annual Azure Consumed Revenue plan, keyed AcrTarget<fullYear>) ----
    public record AcrTargetDto(int FiscalYear, string Label, decimal Target);
    public record AcrTargetUpsert(int FiscalYear, decimal Target);

    /// <summary>Annual ACR targets by fiscal year (end-year label, e.g. FY27). Drives the attainment view.</summary>
    [HttpGet("acr-targets")]
    public async Task<IActionResult> GetAcrTargets(CancellationToken ct)
    {
        var rows = await db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key.StartsWith("AcrTarget"))
            .Select(s => new { s.Key, s.Value })
            .ToListAsync(ct);
        var list = rows
            .Select(x => (Ok: int.TryParse(x.Key["AcrTarget".Length..], out var fy), Fy: fy, x.Value))
            .Where(x => x.Ok)
            .Select(x => new AcrTargetDto(x.Fy, $"FY{x.Fy % 100:00}",
                decimal.TryParse(x.Value, System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : 0m))
            .OrderByDescending(x => x.FiscalYear)
            .ToList();
        return Ok(list);
    }

    [HttpPut("acr-targets")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpsertAcrTargets([FromBody] IReadOnlyList<AcrTargetUpsert> updates, CancellationToken ct)
    {
        if (updates is null) return BadRequest("No targets supplied.");
        var keys = updates.Where(u => u.FiscalYear is >= 2000 and <= 2100).Select(u => $"AcrTarget{u.FiscalYear}").ToList();
        var existing = await db.ApplicationSettings.Where(s => keys.Contains(s.Key)).ToListAsync(ct);
        foreach (var u in updates)
        {
            if (u.FiscalYear is < 2000 or > 2100) continue;
            var key = $"AcrTarget{u.FiscalYear}";
            var row = existing.FirstOrDefault(r => r.Key == key);
            if (u.Target <= 0)
            {
                if (row is not null) db.ApplicationSettings.Remove(row); // clearing a FY removes it
                continue;
            }
            var val = u.Target.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (row is null)
                db.ApplicationSettings.Add(new ApplicationSetting { Key = key, Value = val, Description = $"Annual ACR target for FY{u.FiscalYear % 100:00} (Azure Consumed Revenue, whole dollars)." });
            else
                row.Value = val;
        }
        await db.SaveChangesAsync(ct);
        return await GetAcrTargets(ct);
    }
}
