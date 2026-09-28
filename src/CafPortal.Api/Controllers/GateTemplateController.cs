using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Api.Controllers;

/// <summary>Admin CRUD for the governance gate template (gates + their checklist items).
/// Deactivate hides a gate/item from new work; delete is blocked once any nomination has captured progress on it.</summary>
[ApiController]
[Route("api/gate-template")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class GateTemplateController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet("gates")]
    public async Task<IActionResult> GetGates(CancellationToken ct)
    {
        var gates = await db.GateDefinitions.AsNoTracking().Include(g => g.Items)
            .OrderBy(g => g.Order).ToListAsync(ct);
        var usedItemIds = (await db.NominationGateItems.AsNoTracking()
            .Select(x => x.GateItemDefinitionId).Distinct().ToListAsync(ct)).ToHashSet();
        return Ok(gates.Select(g => new
        {
            g.Id, g.Key, g.Name, g.ExitCriteria, g.Order, g.Weight, g.OwnerRole, g.Active,
            InUse = g.Items.Any(i => usedItemIds.Contains(i.Id)),
            Items = g.Items.OrderBy(i => i.Order).Select(i => new
            {
                i.Id, i.Key, i.Label, Kind = i.Kind.ToString(), i.SubStage, i.ResponsibleRole, i.Mandatory, i.Order, i.Active,
                InUse = usedItemIds.Contains(i.Id),
            }),
        }));
    }

    // ---- Gates ----
    [HttpPost("gates")]
    public async Task<IActionResult> CreateGate([FromBody] GateInput input, CancellationToken ct)
    {
        var key = input.Key?.Trim();
        if (string.IsNullOrEmpty(key)) return BadRequest("Gate key is required.");
        if (await db.GateDefinitions.AnyAsync(g => g.Key == key, ct)) return Conflict($"Gate '{key}' already exists.");
        var maxOrder = await db.GateDefinitions.AnyAsync(ct) ? await db.GateDefinitions.MaxAsync(g => g.Order, ct) : 0;
        var gate = new GateDefinition
        {
            Key = key, Name = input.Name?.Trim() ?? key, ExitCriteria = Trim(input.ExitCriteria),
            Weight = input.Weight ?? 10, OwnerRole = input.OwnerRole?.Trim() ?? "SA", Order = maxOrder + 1,
        };
        db.GateDefinitions.Add(gate);
        await db.SaveChangesAsync(ct);
        return Ok(new { gate.Id });
    }

    [HttpPut("gates/{id:int}")]
    public async Task<IActionResult> UpdateGate(int id, [FromBody] GateInput input, CancellationToken ct)
    {
        var gate = await db.GateDefinitions.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (gate is null) return NotFound();
        var key = input.Key?.Trim();
        if (string.IsNullOrEmpty(key)) return BadRequest("Gate key is required.");
        if (await db.GateDefinitions.AnyAsync(g => g.Key == key && g.Id != id, ct)) return Conflict($"Gate '{key}' already exists.");
        gate.Key = key;
        gate.Name = input.Name?.Trim() ?? gate.Name;
        gate.ExitCriteria = Trim(input.ExitCriteria);
        if (input.Weight is int w) gate.Weight = w;
        if (!string.IsNullOrWhiteSpace(input.OwnerRole)) gate.OwnerRole = input.OwnerRole.Trim();
        if (input.Order is int o) gate.Order = o;
        if (input.Active is bool a) gate.Active = a;
        await db.SaveChangesAsync(ct);
        return Ok(new { gate.Id });
    }

    [HttpDelete("gates/{id:int}")]
    public async Task<IActionResult> DeleteGate(int id, CancellationToken ct)
    {
        var gate = await db.GateDefinitions.Include(g => g.Items).FirstOrDefaultAsync(g => g.Id == id, ct);
        if (gate is null) return NotFound();
        var itemIds = gate.Items.Select(i => i.Id).ToList();
        if (await db.NominationGateItems.AnyAsync(x => itemIds.Contains(x.GateItemDefinitionId), ct))
            return Conflict("This gate has captured progress on some nominations and cannot be deleted. Deactivate it instead.");
        db.GateItemDefinitions.RemoveRange(gate.Items);
        db.GateDefinitions.Remove(gate);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Items ----
    [HttpPost("gates/{gateId:int}/items")]
    public async Task<IActionResult> CreateItem(int gateId, [FromBody] ItemInput input, CancellationToken ct)
    {
        if (!await db.GateDefinitions.AnyAsync(g => g.Id == gateId, ct)) return NotFound();
        var label = input.Label?.Trim();
        if (string.IsNullOrEmpty(label)) return BadRequest("Item label is required.");
        var key = string.IsNullOrWhiteSpace(input.Key) ? Slug(label) : input.Key!.Trim();
        var hasItems = await db.GateItemDefinitions.AnyAsync(i => i.GateDefinitionId == gateId, ct);
        var maxOrder = hasItems ? await db.GateItemDefinitions.Where(i => i.GateDefinitionId == gateId).MaxAsync(i => i.Order, ct) : 0;
        db.GateItemDefinitions.Add(new GateItemDefinition
        {
            GateDefinitionId = gateId, Key = key, Label = label, Kind = ParseKind(input.Kind),
            SubStage = Trim(input.SubStage), ResponsibleRole = input.ResponsibleRole?.Trim() ?? "SA",
            Mandatory = input.Mandatory ?? false, Order = maxOrder + 1,
        });
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPut("items/{id:int}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemInput input, CancellationToken ct)
    {
        var item = await db.GateItemDefinitions.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return NotFound();
        var label = input.Label?.Trim();
        if (string.IsNullOrEmpty(label)) return BadRequest("Item label is required.");
        item.Label = label;
        if (!string.IsNullOrWhiteSpace(input.Key)) item.Key = input.Key.Trim();
        if (!string.IsNullOrWhiteSpace(input.Kind)) item.Kind = ParseKind(input.Kind);
        item.SubStage = Trim(input.SubStage);
        if (!string.IsNullOrWhiteSpace(input.ResponsibleRole)) item.ResponsibleRole = input.ResponsibleRole.Trim();
        if (input.Mandatory is bool m) item.Mandatory = m;
        if (input.Order is int o) item.Order = o;
        if (input.Active is bool a) item.Active = a;
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
    {
        var item = await db.GateItemDefinitions.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return NotFound();
        if (await db.NominationGateItems.AnyAsync(x => x.GateItemDefinitionId == id, ct))
            return Conflict("This item has captured progress and cannot be deleted. Deactivate it instead.");
        db.GateItemDefinitions.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static GateItemKind ParseKind(string? kind)
        => Enum.TryParse<GateItemKind>(kind, ignoreCase: true, out var k) ? k : GateItemKind.Task;

    private static string Slug(string label)
    {
        var chars = label.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        return new string(chars).Replace("--", "-").Trim('-');
    }

    public record GateInput(string? Key, string? Name, string? ExitCriteria, int? Weight, string? OwnerRole, int? Order, bool? Active);
    public record ItemInput(string? Key, string? Label, string? Kind, string? SubStage, string? ResponsibleRole, bool? Mandatory, int? Order, bool? Active);
}
