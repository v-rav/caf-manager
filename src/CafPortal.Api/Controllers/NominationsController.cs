using CafPortal.Application.Abstractions;
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
}
