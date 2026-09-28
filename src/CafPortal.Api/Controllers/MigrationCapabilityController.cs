using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Api.Controllers;

/// <summary>Admin CRUD for the Migration Capability masters (tools, activities, and the tool→supported-activity mapping).</summary>
[ApiController]
[Route("api/capability-master")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class MigrationCapabilityController(IApplicationDbContext db) : ControllerBase
{
    // ---- Tools ----
    [HttpGet("tools")]
    public async Task<IActionResult> GetTools(CancellationToken ct)
    {
        var tools = await db.MigrationTools.AsNoTracking()
            .OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);
        var map = await db.MigrationToolActivities.AsNoTracking().Select(m => new { m.ToolId, m.ActivityId }).ToListAsync(ct);
        var byTool = map.GroupBy(m => m.ToolId).ToDictionary(g => g.Key, g => g.Select(x => x.ActivityId).ToArray());
        return Ok(tools.Select(t => new
        {
            t.Id, t.Name, t.Category, t.Vendor, t.SortOrder, Active = t.ActiveFlag,
            SupportedActivityIds = byTool.TryGetValue(t.Id, out var ids) ? ids : Array.Empty<int>(),
        }));
    }

    [HttpPost("tools")]
    public async Task<IActionResult> CreateTool([FromBody] ToolInput input, CancellationToken ct)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Tool name is required.");
        if (await db.MigrationTools.AnyAsync(t => t.Name == name, ct)) return Conflict($"Tool '{name}' already exists.");
        var maxSort = await db.MigrationTools.AnyAsync(ct) ? await db.MigrationTools.MaxAsync(t => t.SortOrder, ct) : 0;
        var tool = new MigrationTool { Name = name, Category = input.Category?.Trim() ?? "Other", Vendor = Trim(input.Vendor), SortOrder = maxSort + 1 };
        db.MigrationTools.Add(tool);
        await db.SaveChangesAsync(ct);
        return Ok(new { tool.Id });
    }

    [HttpPut("tools/{id:int}")]
    public async Task<IActionResult> UpdateTool(int id, [FromBody] ToolInput input, CancellationToken ct)
    {
        var tool = await db.MigrationTools.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tool is null) return NotFound();
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Tool name is required.");
        if (await db.MigrationTools.AnyAsync(t => t.Name == name && t.Id != id, ct)) return Conflict($"Tool '{name}' already exists.");
        tool.Name = name;
        tool.Category = input.Category?.Trim() ?? tool.Category;
        tool.Vendor = Trim(input.Vendor);
        if (input.Active is bool active) tool.ActiveFlag = active;
        await db.SaveChangesAsync(ct);
        return Ok(new { tool.Id });
    }

    [HttpDelete("tools/{id:int}")]
    public async Task<IActionResult> DeleteTool(int id, CancellationToken ct)
    {
        var tool = await db.MigrationTools.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tool is null) return NotFound();
        if (await db.NominationToolUsages.AnyAsync(u => u.ToolId == id, ct))
            return Conflict("This tool has captured usage and cannot be deleted. Deactivate it instead.");
        db.MigrationToolActivities.RemoveRange(db.MigrationToolActivities.Where(m => m.ToolId == id));
        db.MigrationTools.Remove(tool);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Replaces the tool's supported-activity set. Empty = supports any activity.</summary>
    [HttpPut("tools/{id:int}/activities")]
    public async Task<IActionResult> SetSupported(int id, [FromBody] SupportedInput input, CancellationToken ct)
    {
        if (!await db.MigrationTools.AnyAsync(t => t.Id == id, ct)) return NotFound();
        db.MigrationToolActivities.RemoveRange(db.MigrationToolActivities.Where(m => m.ToolId == id));
        var activityIds = (input.ActivityIds ?? Array.Empty<int>()).Distinct();
        var valid = await db.MigrationActivities.Where(a => activityIds.Contains(a.Id)).Select(a => a.Id).ToListAsync(ct);
        foreach (var aid in valid)
            db.MigrationToolActivities.Add(new MigrationToolActivity { ToolId = id, ActivityId = aid });
        await db.SaveChangesAsync(ct);
        return Ok(new { count = valid.Count });
    }

    // ---- Activities ----
    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities(CancellationToken ct)
        => Ok(await db.MigrationActivities.AsNoTracking()
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .Select(a => new { a.Id, a.Name, a.Stage, a.SortOrder, Active = a.ActiveFlag })
            .ToListAsync(ct));

    [HttpPost("activities")]
    public async Task<IActionResult> CreateActivity([FromBody] ActivityInput input, CancellationToken ct)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Activity name is required.");
        if (await db.MigrationActivities.AnyAsync(a => a.Name == name, ct)) return Conflict($"Activity '{name}' already exists.");
        var maxSort = await db.MigrationActivities.AnyAsync(ct) ? await db.MigrationActivities.MaxAsync(a => a.SortOrder, ct) : 0;
        var activity = new MigrationActivity { Name = name, Stage = Trim(input.Stage), SortOrder = maxSort + 1 };
        db.MigrationActivities.Add(activity);
        await db.SaveChangesAsync(ct);
        return Ok(new { activity.Id });
    }

    [HttpPut("activities/{id:int}")]
    public async Task<IActionResult> UpdateActivity(int id, [FromBody] ActivityInput input, CancellationToken ct)
    {
        var activity = await db.MigrationActivities.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (activity is null) return NotFound();
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return BadRequest("Activity name is required.");
        if (await db.MigrationActivities.AnyAsync(a => a.Name == name && a.Id != id, ct)) return Conflict($"Activity '{name}' already exists.");
        activity.Name = name;
        activity.Stage = Trim(input.Stage);
        if (input.Active is bool active) activity.ActiveFlag = active;
        await db.SaveChangesAsync(ct);
        return Ok(new { activity.Id });
    }

    [HttpDelete("activities/{id:int}")]
    public async Task<IActionResult> DeleteActivity(int id, CancellationToken ct)
    {
        var activity = await db.MigrationActivities.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (activity is null) return NotFound();
        if (await db.NominationToolUsages.AnyAsync(u => u.ActivityId == id, ct))
            return Conflict("This activity has captured usage and cannot be deleted. Deactivate it instead.");
        db.MigrationToolActivities.RemoveRange(db.MigrationToolActivities.Where(m => m.ActivityId == id));
        db.MigrationActivities.Remove(activity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public record ToolInput(string? Name, string? Category, string? Vendor, bool? Active);
    public record ActivityInput(string? Name, string? Stage, bool? Active);
    public record SupportedInput(int[]? ActivityIds);
}
