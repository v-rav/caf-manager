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
}
