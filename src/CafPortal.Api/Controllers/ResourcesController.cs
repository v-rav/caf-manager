using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ResourcesController(IResourceService resources) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ResourceQuery query, CancellationToken ct)
        => Ok(await resources.GetAllAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var resource = await resources.GetByIdAsync(id, ct);
        return resource is null ? NotFound() : Ok(resource);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Create([FromBody] ResourceUpsertDto input, CancellationToken ct)
    {
        var created = await resources.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.ResourceId }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Update(int id, [FromBody] ResourceUpsertDto input, CancellationToken ct)
    {
        var updated = await resources.UpdateAsync(id, input, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => await resources.DeleteAsync(id, ct) ? NoContent() : NotFound();

    /// <summary>Assign (or re-map) an account to this resource.</summary>
    [HttpPost("{id:int}/accounts")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> AssignAccount(int id, [FromBody] AssignAccountDto input, CancellationToken ct)
        => await resources.AssignAccountAsync(id, input, ct) ? NoContent() : NotFound();

    /// <summary>Remove an account mapping from this resource.</summary>
    [HttpDelete("{id:int}/accounts/{accountId:int}")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> UnassignAccount(int id, int accountId, CancellationToken ct)
        => await resources.UnassignAccountAsync(id, accountId, ct) ? NoContent() : NotFound();
}
