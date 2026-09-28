using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Capability mapping: which activities a tool CAN support (not that it was used — that's NominationToolUsage).
/// An empty mapping for a tool means it supports any activity (e.g. Partner/Customer/Internal tooling).</summary>
public class MigrationToolActivity : AuditableEntity
{
    public int Id { get; set; }
    public int ToolId { get; set; }
    public int ActivityId { get; set; }
}
