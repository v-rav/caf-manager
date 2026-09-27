using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Auth;

/// <summary>Portal role. Admin manages users/config; Lead has read-across governance; SA owns nominations.</summary>
public enum UserRole { Admin = 0, Lead = 1, Sa = 2 }

/// <summary>A custom portal login (username + PBKDF2 hash). Provides identity for accountability/updated-by.</summary>
public class AppUser : AuditableEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Sa;
    public bool Active { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginUtc { get; set; }
}
