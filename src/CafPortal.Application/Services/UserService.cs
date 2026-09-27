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

    public async Task<ProvisionResult> ProvisionSolutionArchitectsAsync(CancellationToken ct = default)
    {
        var names = await _db.Nominations.AsNoTracking()
            .Where(n => n.SolutionArchitect != null && n.SolutionArchitect != "")
            .Select(n => n.SolutionArchitect!)
            .Distinct()
            .ToListAsync(ct);

        var existing = await _db.AppUsers.AsNoTracking().Select(u => new { u.Username, u.DisplayName }).ToListAsync(ct);
        var existingDisplay = existing.Select(u => u.DisplayName.Trim().ToLowerInvariant()).ToHashSet();
        var usernames = existing.Select(u => u.Username.ToLowerInvariant()).ToHashSet();

        const string temp = "Sa@12345";
        var hash = PasswordHashing.Hash(temp);
        var created = new List<ProvisionedUser>();
        var skipped = 0;

        foreach (var name in names.Select(n => n.Trim()).Where(n => n.Length > 0).OrderBy(n => n))
        {
            if (existingDisplay.Contains(name.ToLowerInvariant())) { skipped++; continue; }
            var baseSlug = Slug(name);
            var slug = baseSlug;
            var i = 2;
            while (usernames.Contains(slug)) slug = $"{baseSlug}{i++}";
            usernames.Add(slug);
            existingDisplay.Add(name.ToLowerInvariant());
            _db.AppUsers.Add(new AppUser
            {
                Username = slug, DisplayName = name, Role = UserRole.Sa, Active = true,
                PasswordHash = hash, MustChangePassword = true,
            });
            created.Add(new ProvisionedUser(slug, name));
        }
        if (created.Count > 0) await _db.SaveChangesAsync(ct);
        return new ProvisionResult(created.Count, skipped, temp, created);
    }

    // Turn a display name into a stable username slug (letters/digits, dot-separated).
    private static string Slug(string name)
    {
        var chars = name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '.').ToArray();
        var s = new string(chars);
        while (s.Contains("..")) s = s.Replace("..", ".");
        s = s.Trim('.');
        return string.IsNullOrEmpty(s) ? "sa" : s;
    }
}
