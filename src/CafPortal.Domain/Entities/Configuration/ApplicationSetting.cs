using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Generic key/value application setting (DefaultCapacityLimit, RefreshTime, thresholds, ...).</summary>
public class ApplicationSetting : AuditableEntity
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
