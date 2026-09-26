using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? region, CancellationToken ct)
        => Ok(await analytics.GetAsync(region, ct));
}
