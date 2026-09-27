using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/governance")]
[Produces("application/json")]
[Authorize]
public class GovernanceBoardController(IGovernanceService governance) : ControllerBase
{
    [HttpGet("blockers")]
    public async Task<IActionResult> OpenBlockers([FromQuery] string? region, CancellationToken ct)
        => Ok(await governance.GetOpenBlockersAsync(region, ct));

    [HttpGet("blocker-categories")]
    public IActionResult Categories() => Ok(governance.GetBlockerCategories());
}
