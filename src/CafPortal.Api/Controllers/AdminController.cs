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

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
        => Ok(await refresh.ReseedAsync(ct));

    [HttpPost("merge-accounts")]
    public async Task<IActionResult> MergeAccounts([FromQuery] bool apply = false, CancellationToken ct = default)
        => Ok(await refresh.MergeDuplicateAccountsAsync(apply, ct));

    [HttpPost("park-accounts")]
    public async Task<IActionResult> ParkAccounts([FromQuery] bool apply = false, CancellationToken ct = default)
        => Ok(await refresh.ParkNoTpidAccountsAsync(apply, ct));

    [HttpPost("unpark-accounts")]
    public async Task<IActionResult> UnparkAccounts(CancellationToken ct = default)
        => Ok(await refresh.UnparkAllAccountsAsync(ct));

    [HttpPost("import-offerings")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> ImportOfferings([FromForm] IFormFile file, [FromQuery] bool apply = false, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");
        await using var stream = file.OpenReadStream();
        return Ok(await refresh.ImportOfferingsAsync(stream, apply, ct));
    }

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

    /// <summary>Upload any of the 3 nomination workbooks; the type is auto-detected from its columns.</summary>
    [HttpPost("upload-auto")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> UploadAuto([FromForm] IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");
        await using var stream = file.OpenReadStream();
        return Ok(await refresh.UploadAutoAsync(stream, file.FileName, ct));
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
        => Ok(await refresh.GetStatusAsync(ct));
}
