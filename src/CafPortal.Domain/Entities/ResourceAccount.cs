using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

public class ResourceAccount : AuditableEntity
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public int AccountId { get; set; }
    public string? Source { get; set; }
    public string? RelationshipType { get; set; }

    public Resource? Resource { get; set; }
    public Account? Account { get; set; }
}
