using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

/// <summary>Data-quality view over the resource→account mappings that feed capacity (Admin/Lead; fixes Admin-only).</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin,Lead")]
public class ReconciliationController(IReconciliationService reconciliation) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? region, CancellationToken ct)
        => Ok(await reconciliation.GetAsync(region, ct));

    /// <summary>Seed assignments from the in-flight links. Defaults to a read-only preview (apply=false).</summary>
    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Seed([FromQuery] string? region, [FromQuery] bool apply, CancellationToken ct)
        => Ok(await reconciliation.SeedAssignmentsAsync(region, apply, ct));

    /// <summary>People named in FDO ownership fields with no matching portal resource (candidates to add).</summary>
    [HttpGet("unmatched-people")]
    public async Task<IActionResult> UnmatchedPeople([FromQuery] string? region, CancellationToken ct)
        => Ok(await reconciliation.GetUnmatchedPeopleAsync(region, ct));

    /// <summary>Repoint near-duplicate resource→account links onto their master. Preview by default (apply=false).</summary>
    [HttpPost("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cleanup([FromQuery] string? region, [FromQuery] bool apply, CancellationToken ct)
        => Ok(await reconciliation.CleanupLinksAsync(region, apply, ct));
}
