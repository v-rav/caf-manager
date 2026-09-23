using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

public class RoleConfiguration : AuditableEntity
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool ActiveFlag { get; set; } = true;

    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}
