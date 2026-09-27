using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NominationsController(INominationService nominations) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? region, [FromQuery] string? status, CancellationToken ct)
        => Ok(await nominations.GetAsync(region, status, ct));

    [HttpGet("vocab")]
    public IActionResult Vocab() => Ok(new
    {
        classifications = new[] { "Standard Factory", "Strategic Pilot", "Lighthouse Engagement", "Innovation / POC", "Recovery Engagement" },
        velocityImpacts = new[] { "Low", "Medium", "High", "Critical" },
    });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] NominationUpdateDto input, CancellationToken ct)
    {
        var updated = await nominations.UpdateAsync(id, input, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("{id:int}/waves")]
    public async Task<IActionResult> AddWave(int id, [FromBody] WaveLinkUpsertDto input, CancellationToken ct)
    {
        var link = await nominations.AddWaveLinkAsync(id, input, ct);
        return link is null ? BadRequest("Nomination not found or reference missing.") : Ok(link);
    }

    [HttpDelete("{id:int}/waves/{waveId:int}")]
    public async Task<IActionResult> DeleteWave(int id, int waveId, CancellationToken ct)
        => await nominations.DeleteWaveLinkAsync(id, waveId, ct) ? NoContent() : NotFound();

    [HttpPost("{id:int}/resources")]
    public async Task<IActionResult> AssignResource(int id, [FromBody] AssignResourceDto input, CancellationToken ct)
    {
        var link = await nominations.AssignResourceAsync(id, input, ct);
        return link is null ? BadRequest("Nomination or resource not found.") : Ok(link);
    }

    [HttpDelete("{id:int}/resources/{resourceId:int}")]
    public async Task<IActionResult> UnassignResource(int id, int resourceId, CancellationToken ct)
        => await nominations.UnassignResourceAsync(id, resourceId, ct) ? NoContent() : NotFound();
}
