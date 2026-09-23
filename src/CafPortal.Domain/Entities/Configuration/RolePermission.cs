using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Permission granted to a role. Stored in config so no permission is hardcoded.</summary>
public class RolePermission : AuditableEntity
{
    public int Id { get; set; }
    public int RoleConfigurationId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public bool Granted { get; set; } = true;

    public RoleConfiguration? Role { get; set; }
}
