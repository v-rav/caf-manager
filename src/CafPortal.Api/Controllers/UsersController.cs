using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await users.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserUpsert input, CancellationToken ct)
    {
        var user = await users.CreateAsync(input, ct);
        return user is null ? Conflict(new { message = "Username is taken or password missing." }) : Ok(user);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpsert input, CancellationToken ct)
    {
        var user = await users.UpdateAsync(id, input, ct);
        return user is null ? Conflict(new { message = "User not found or username collides." }) : Ok(user);
    }

    public record ResetPasswordRequest(string NewPassword);

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        var ok = await users.ResetPasswordAsync(id, req.NewPassword ?? "", ct);
        return ok ? Ok(new { ok = true }) : NotFound();
    }
}
