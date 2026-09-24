using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LeaveController(ILeaveService leave) : ControllerBase
{
    /// <summary>Leave items within an upcoming window (default 30 days).</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int windowDays = 30, [FromQuery] string? region = null, CancellationToken ct = default)
        => Ok(await leave.GetWindowAsync(windowDays, region, ct));

    /// <summary>Resources with active accounts who have upcoming leave (coverage clash risk).</summary>
    [HttpGet("clashes")]
    public async Task<IActionResult> GetClashes([FromQuery] string? region = null, CancellationToken ct = default)
        => Ok(await leave.GetClashesAsync(region, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeaveUpsertDto input, CancellationToken ct)
    {
        var created = await leave.CreateAsync(input, ct);
        return created is null ? NotFound($"Resource {input.ResourceId} not found.") : Ok(created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] LeaveUpsertDto input, CancellationToken ct)
    {
        var updated = await leave.UpdateAsync(id, input, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => await leave.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
