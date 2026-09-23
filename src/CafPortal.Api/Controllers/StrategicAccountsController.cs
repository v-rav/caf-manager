using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/strategicaccounts")]
[Produces("application/json")]
public class StrategicAccountsController(IStrategicAccountService strategic) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? region, CancellationToken ct)
        => Ok(await strategic.GetAsync(region, ct));
}
