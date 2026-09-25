using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

/// <summary>Import audit trail — what each FDO upload / refresh changed.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ImportsController(IImportHistoryService history) : ControllerBase
{
    /// <summary>Recent import runs (most recent first) with change counts.</summary>
    [HttpGet]
    public async Task<IActionResult> Runs([FromQuery] int take = 100, CancellationToken ct = default)
        => Ok(await history.GetRunsAsync(take, ct));

    /// <summary>Row-level changes recorded during one import run.</summary>
    [HttpGet("{runId:int}/changes")]
    public async Task<IActionResult> Changes(int runId, CancellationToken ct)
        => Ok(await history.GetChangesAsync(runId, ct));
}
