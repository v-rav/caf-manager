using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

/// <summary>Database backup (download a compressed snapshot) and restore (replace the live DB from a backup zip).</summary>
[ApiController]
[Route("api/[controller]")]
public class BackupController(IBackupService backup) : ControllerBase
{
    private const string Zip = "application/zip";

    /// <summary>Downloads a consistent, zipped snapshot of the current database.</summary>
    [HttpGet("download")]
    public async Task<IActionResult> Download(CancellationToken ct)
    {
        var (content, fileName) = await backup.CreateBackupAsync(ct);
        return File(content, Zip, fileName);
    }

    /// <summary>Restores the database from an uploaded backup zip. Destructive — replaces all current data.</summary>
    [HttpPost("restore")]
    [RequestSizeLimit(104_857_600)] // 100 MB
    public async Task<IActionResult> Restore([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");
        await using var stream = file.OpenReadStream();
        var result = await backup.RestoreAsync(stream, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
