using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/acr-recovery")]
[Produces("application/json")]
[Authorize]
public class AcrRecoveryController(IAcrRecoveryService recovery) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? region, CancellationToken ct)
        => Ok(await recovery.GetAllAsync(region, ct));

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] string? region, CancellationToken ct)
        => Ok(await recovery.GetSummaryAsync(region, ct));

    [HttpPost("flag")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Flag([FromBody] AcrRecoveryFlagRequest req, CancellationToken ct)
        => await recovery.FlagAsync(req, ct) ? Ok() : NotFound();

    [HttpPost("{id:int}/notify")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Notify(int id, CancellationToken ct)
        => await recovery.MarkNotifiedAsync(id, ct) ? Ok() : NotFound();

    [HttpPost("{id:int}/close")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Close(int id, [FromBody] CloseRecoveryRequest req, CancellationToken ct)
        => await recovery.CloseAsync(id, req?.Note, ct) ? Ok() : NotFound();

    [HttpPost("reconcile")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Reconcile(CancellationToken ct)
        => Ok(new { realized = await recovery.ReconcileAsync(ct) });
}

public record CloseRecoveryRequest(string? Note);
