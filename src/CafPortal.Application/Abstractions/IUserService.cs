using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

/// <summary>Custom-login user management + credential validation.</summary>
public interface IUserService
{
    Task<UserDto?> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default);
    Task MarkLoggedInAsync(int userId, CancellationToken ct = default);
    Task<UserDto?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default);

    /// <summary>Create a user. Returns null when the username is already taken.</summary>
    Task<UserDto?> CreateAsync(UserUpsert input, CancellationToken ct = default);
    /// <summary>Update a user. Returns null when not found or the username collides.</summary>
    Task<UserDto?> UpdateAsync(int id, UserUpsert input, CancellationToken ct = default);

    /// <summary>Change own password after verifying the current one. False when the current password is wrong.</summary>
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default);
    /// <summary>Admin reset — sets a new password and forces a change at next login.</summary>
    Task<bool> ResetPasswordAsync(int userId, string newPassword, CancellationToken ct = default);
}
