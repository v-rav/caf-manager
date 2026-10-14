using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Api.Controllers;

/// <summary>Admin editing of the data-driven FDO import profiles (file signatures) and column header maps.</summary>
[ApiController]
[Route("api/import-profiles")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class ImportProfilesController(IApplicationDbContext db, ICurrentUser currentUser) : ControllerBase
{
    public record ProfileInput(string Label, int DetectOrder, string Signature);
    public record ColumnInput(string Candidates);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var profiles = await db.ImportProfiles.AsNoTracking().OrderBy(p => p.DetectOrder)
            .Select(p => new { p.Id, p.Kind, p.Label, p.DetectOrder, p.Signature, p.UpdatedBy, p.UpdatedUtc }).ToListAsync(ct);
        var columns = await db.ImportColumnMaps.AsNoTracking().OrderBy(c => c.Kind).ThenBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Kind, c.Field, c.Candidates, c.UpdatedBy, c.UpdatedUtc }).ToListAsync(ct);
        var defaults = ImportProfiles.DefaultColumns.ToDictionary(c => $"{c.Kind}|{c.Field}", c => c.Candidates);
        return Ok(new
        {
            Profiles = profiles,
            Columns = columns.Select(c => new
            {
                c.Id, c.Kind, c.Field, c.Candidates, c.UpdatedBy, c.UpdatedUtc,
                Default = defaults.GetValueOrDefault($"{c.Kind}|{c.Field}"),
            }),
        });
    }

    [HttpPut("profiles/{id:int}")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] ProfileInput input, CancellationToken ct)
    {
        var p = await db.ImportProfiles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Label)) return BadRequest("Label is required.");
        if (!input.Signature.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any())
            return BadRequest("Signature needs at least one header (clauses ';', alternatives '|').");
        p.Label = input.Label.Trim();
        p.DetectOrder = input.DetectOrder;
        p.Signature = input.Signature.Trim();
        p.UpdatedBy = currentUser.Name;
        p.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await ImportProfileStore.LoadAsync(db, ct);
        return Ok();
    }

    [HttpPut("columns/{id:int}")]
    public async Task<IActionResult> UpdateColumn(int id, [FromBody] ColumnInput input, CancellationToken ct)
    {
        var c = await db.ImportColumnMaps.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        var candidates = string.Join("|", (input.Candidates ?? string.Empty)
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (candidates.Length == 0) return BadRequest("At least one header candidate is required.");
        c.Candidates = candidates;
        c.UpdatedBy = currentUser.Name;
        c.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await ImportProfileStore.LoadAsync(db, ct);
        return Ok();
    }
}
