using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

/// <summary>Ownership Effectiveness scorecard (PM / SA / CFTL), scored on the server from the Scoring-models settings.</summary>
[ApiController]
[Route("api/effectiveness")]
[Produces("application/json")]
[Authorize(Roles = "Admin,Lead")]
public class EffectivenessController(IEffectivenessService effectiveness) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? region, [FromQuery] string role = "PM", [FromQuery] int? fy = null,
        CancellationToken ct = default)
        => Ok(await effectiveness.GetAsync(region, role, fy, ct));
}
