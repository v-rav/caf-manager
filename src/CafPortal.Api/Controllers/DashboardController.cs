using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Executive KPI cards and chart series. Optional region scopes the roll-up.</summary>
    [HttpGet("executive")]
    public async Task<IActionResult> GetExecutive([FromQuery] string? region, CancellationToken ct)
        => Ok(await dashboard.GetExecutiveAsync(region, ct));
}
