using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

/// <summary>Administrative operations. Triggers an on-demand data refresh (imports + capacity rebuild).</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AdminController(IDataRefreshService refresh) : ControllerBase
{
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
        => Ok(await refresh.RefreshAsync(ct));
}
