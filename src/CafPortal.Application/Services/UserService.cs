using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class UserService(IApplicationDbContext db) : IUserService
{
    private readonly IApplicationDbContext _db = db;

    private static UserDto Map(AppUser u) => new(u.Id, u.Username, u.DisplayName, u.Role, u.Active, u.MustChangePassword, u.LastLoginUtc);

    public async Task<UserDto?> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Username == username && u.Active, ct);
        if (user is null || !PasswordHashing.Verify(password, user.PasswordHash)) return null;
        return Map(user);
    }

    public async Task MarkLoggedInAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;
        user.LastLoginUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<UserDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? null : Map(user);
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default) =>
        await _db.AppUsers.AsNoTracking().OrderBy(u => u.Username)
            .Select(u => new UserDto(u.Id, u.Username, u.DisplayName, u.Role, u.Active, u.MustChangePassword, u.LastLoginUtc))
            .ToListAsync(ct);

    public async Task<UserDto?> CreateAsync(UserUpsert input, CancellationToken ct = default)
    {
        var username = input.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(input.Password)) return null;
        if (await _db.AppUsers.AnyAsync(u => u.Username == username, ct)) return null;
        var user = new AppUser
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(input.DisplayName) ? username : input.DisplayName.Trim(),
            Role = input.Role,
            Active = input.Active,
            PasswordHash = PasswordHashing.Hash(input.Password),
            MustChangePassword = true,
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<UserDto?> UpdateAsync(int id, UserUpsert input, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;
        var username = input.Username.Trim();
        if (!string.IsNullOrWhiteSpace(username) && username != user.Username)
        {
            if (await _db.AppUsers.AnyAsync(u => u.Username == username && u.Id != id, ct)) return null;
            user.Username = username;
        }
        user.DisplayName = string.IsNullOrWhiteSpace(input.DisplayName) ? user.DisplayName : input.DisplayName.Trim();
        user.Role = input.Role;
        user.Active = input.Active;
        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            user.PasswordHash = PasswordHashing.Hash(input.Password);
            user.MustChangePassword = true;
        }
        await _db.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || string.IsNullOrWhiteSpace(newPassword)) return false;
        if (!PasswordHashing.Verify(currentPassword, user.PasswordHash)) return false;
        user.PasswordHash = PasswordHashing.Hash(newPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || string.IsNullOrWhiteSpace(newPassword)) return false;
        user.PasswordHash = PasswordHashing.Hash(newPassword);
        user.MustChangePassword = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
