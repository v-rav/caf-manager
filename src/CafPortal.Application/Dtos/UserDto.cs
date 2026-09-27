using CafPortal.Domain.Entities.Auth;

namespace CafPortal.Application.Dtos;

/// <summary>A portal user (never carries the password hash).</summary>
public record UserDto(int Id, string Username, string DisplayName, UserRole Role, bool Active, bool MustChangePassword, DateTime? LastLoginUtc);

public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Create/update a user. Password is optional on update (blank = keep existing).</summary>
public record UserUpsert(string Username, string DisplayName, UserRole Role, bool Active, string? Password);

/// <summary>Result of bulk-provisioning SA logins from the Solution Architects present in the data.</summary>
public record ProvisionResult(int Created, int Skipped, string TempPassword, IReadOnlyList<ProvisionedUser> Users);
public record ProvisionedUser(string Username, string DisplayName);
