using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

/// <summary>Maps abbreviations / variants to a canonical account name (e.g. "SG" -> "Societe Generale").</summary>
public class AccountAlias : AuditableEntity
{
    public int AliasId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string StandardAccountName { get; set; } = string.Empty;
}
