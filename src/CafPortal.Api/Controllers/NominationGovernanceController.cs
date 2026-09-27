using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/nominations/{nominationId:int}/governance")]
[Produces("application/json")]
[Authorize]
public class NominationGovernanceController(IGovernanceService governance) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int nominationId, CancellationToken ct)
    {
        var dto = await governance.GetForNominationAsync(nominationId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("items/{itemDefId:int}")]
    public async Task<IActionResult> UpdateItem(int nominationId, int itemDefId, [FromBody] UpdateGateItemRequest req, CancellationToken ct)
    {
        var dto = await governance.UpdateItemAsync(nominationId, itemDefId, req, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("blockers")]
    public async Task<IActionResult> RaiseBlocker(int nominationId, [FromBody] RaiseBlockerRequest req, CancellationToken ct)
    {
        var dto = await governance.RaiseBlockerAsync(nominationId, req, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("blockers/{blockerId:int}/resolve")]
    public async Task<IActionResult> ResolveBlocker(int nominationId, int blockerId, CancellationToken ct)
    {
        var dto = await governance.ResolveBlockerAsync(nominationId, blockerId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(int nominationId, CancellationToken ct)
        => Ok(await governance.GetEventsAsync(nominationId, ct));

    [HttpPost("milestones")]
    public async Task<IActionResult> AddMilestone(int nominationId, [FromBody] MilestoneUpsert req, CancellationToken ct)
    {
        var dto = await governance.AddMilestoneAsync(nominationId, req, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("milestones/{milestoneId:int}")]
    public async Task<IActionResult> DeleteMilestone(int nominationId, int milestoneId, CancellationToken ct)
    {
        var dto = await governance.DeleteMilestoneAsync(nominationId, milestoneId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
