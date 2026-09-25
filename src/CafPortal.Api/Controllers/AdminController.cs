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

    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromQuery] string kind = "nominations", CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");
        await using var stream = file.OpenReadStream();
        var result = await refresh.UploadAndRefreshAsync(kind, stream, file.FileName, ct);
        return Ok(result);
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
        => Ok(await refresh.GetStatusAsync(ct));
}
