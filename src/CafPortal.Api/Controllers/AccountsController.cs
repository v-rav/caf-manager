using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AccountsController(IAccountService accounts) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] string? region, CancellationToken ct)
        => Ok(await accounts.GetAllAsync(search, region, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(id, ct);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Create([FromBody] AccountUpsertDto input, CancellationToken ct)
    {
        var created = await accounts.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.AccountId }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Update(int id, [FromBody] AccountUpsertDto input, CancellationToken ct)
    {
        var updated = await accounts.UpdateAsync(id, input, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => await accounts.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
