using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/governance")]
[Produces("application/json")]
[Authorize]
public class GovernanceBoardController(IGovernanceService governance, ILookupService lookups) : ControllerBase
{
    [HttpGet("blockers")]
    public async Task<IActionResult> OpenBlockers([FromQuery] string? region, CancellationToken ct)
        => Ok(await governance.GetOpenBlockersAsync(region, ct));

    [HttpGet("blocker-categories")]
    public async Task<IActionResult> Categories(CancellationToken ct)
    {
        var values = await lookups.ValuesAsync("BlockerCategory", ct);
        return Ok(values.Count > 0 ? values : governance.GetBlockerCategories());
    }

    [HttpGet("blocker-owners")]
    public async Task<IActionResult> Owners(CancellationToken ct)
        => Ok(await lookups.ValuesAsync("BlockerOwner", ct));

    [HttpGet("milestone-types")]
    public async Task<IActionResult> MilestoneTypes(CancellationToken ct)
        => Ok(await lookups.ValuesAsync("Milestone", ct));

    [HttpGet("migration-tools")]
    public async Task<IActionResult> MigrationTools(CancellationToken ct)
        => Ok(await governance.GetMigrationToolsAsync(ct));

    [HttpGet("migration-activities")]
    public async Task<IActionResult> MigrationActivities(CancellationToken ct)
        => Ok(await governance.GetMigrationActivitiesAsync(ct));

    [HttpGet("capability")]
    public async Task<IActionResult> Capability([FromQuery] string? region, CancellationToken ct)
        => Ok(await governance.GetCapabilityAsync(region, ct));
}
