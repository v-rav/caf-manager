namespace CafPortal.Domain.Common;

/// <summary>Base type carrying audit timestamps for all persisted entities.</summary>
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; }
}
