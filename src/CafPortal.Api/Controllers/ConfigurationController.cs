using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Api.Controllers;

/// <summary>Exposes configuration metadata (regions, roles) that drives the UI without code changes.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
            .Select(r => new
            {
                r.RoleName,
                r.Description,
                Permissions = r.Permissions.Where(p => p.Granted).Select(p => p.PermissionKey)
            })
            .ToListAsync(ct));

    /// <summary>Optimal active-account capacity each role type can handle.</summary>
    [HttpGet("capacity")]
    public async Task<IActionResult> GetCapacity(CancellationToken ct)
        => Ok(await BuildCapacityAsync(ct));
    [HttpPut("capacity")]
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
}
