using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

/// <summary>Append-only log of ownership changes on an account (who owned which role, when).</summary>
public class OwnershipHistory : AuditableEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }

    /// <summary>Ownership role changed (e.g. PM, SA, Account Owner, Backup Owner).</summary>
    public string Role { get; set; } = string.Empty;
    public string? PreviousOwner { get; set; }
    public string? NewOwner { get; set; }
    public DateOnly ChangedOn { get; set; }
    public string? Notes { get; set; }

    public Account? Account { get; set; }
}
