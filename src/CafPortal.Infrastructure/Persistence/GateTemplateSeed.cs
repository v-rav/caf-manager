using CafPortal.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

/// <summary>Seeds the 8-gate SA-accountability template (structural, editable later). Idempotent: runs only when empty.</summary>
public static class GateTemplateSeed
{
    // (gate items): Key, Label, Kind, SubStage, ResponsibleRole, Mandatory
    private static readonly (string Key, string Name, int Weight, string? Exit, (string Key, string Label, GateItemKind Kind, string? Sub, string Role, bool Mandatory)[] Items)[] Template =
    {
        ("G1", "Discovery & Readiness", 10, "Discovery complete · Dependency map · Risks captured · Readiness updated", new (string, string, GateItemKind, string?, string, bool)[]
        {
            ("discovery", "Customer discovery sessions", GateItemKind.Task, null, "SA", false),
            ("inventory", "App inventory validated", GateItemKind.Task, null, "SA", false),
            ("deps", "Dependencies identified", GateItemKind.Task, null, "SA", false),
            ("arch", "Hosting architecture understood", GateItemKind.Task, null, "SA", false),
            ("risks", "Risks & assumptions documented", GateItemKind.Deliverable, null, "SA", false),
        }),
        ("G2", "Prerequisites", 15, "Prereq tracker complete · Owners identified · Open blockers visible", new (string, string, GateItemKind, string?, string, bool)[]
        {
            ("license", "GHCP Enterprise License", GateItemKind.Prerequisite, null, "Customer", false),
            ("repo", "Repository access", GateItemKind.Prerequisite, null, "Customer", false),
            ("env", "Environment access", GateItemKind.Prerequisite, null, "Customer", false),
            ("lz", "Landing zone ready", GateItemKind.Prerequisite, null, "SA", false),
            ("security", "Security review scheduled", GateItemKind.Prerequisite, null, "SA", false),
            ("cicd", "CI/CD readiness", GateItemKind.Prerequisite, null, "SA", false),
        }),
        ("G3", "Assessment", 15, "Assessment report · Migration strategy approved · Risk register", new (string, string, GateItemKind, string?, string, bool)[]
        {
            ("appcat", "AppCAT / Azure Migrate assessment", GateItemKind.Task, null, "SA", false),
            ("approach", "Migration approach finalized", GateItemKind.Task, null, "SA", false),
            ("effort", "Effort sizing", GateItemKind.Task, null, "SA", false),
            ("risks", "Technical risks documented", GateItemKind.Deliverable, null, "SA", false),
            ("report", "Assessment report", GateItemKind.Deliverable, null, "SA", false),
        }),
        ("G4", "Scope Governance", 20, "Signed scope document · No ownership ambiguity · FDO updated", new (string, string, GateItemKind, string?, string, bool)[]
        {
            ("doc", "Scope document created", GateItemKind.Deliverable, null, "SA", false),
            ("in", "In-scope defined", GateItemKind.Task, null, "SA", false),
            ("out", "Out-of-scope defined", GateItemKind.Task, null, "SA", false),
            ("custresp", "Customer responsibilities", GateItemKind.Task, null, "Customer", false),
            ("factresp", "Factory responsibilities", GateItemKind.Task, null, "SA", false),
            ("accept", "Acceptance criteria", GateItemKind.Task, null, "SA", false),
            ("signed", "Signed scope document", GateItemKind.Signoff, null, "Customer", true),
        }),
        ("G5", "Architecture", 10, "TAD approved · Customer approval · Architecture risks closed", new (string, string, GateItemKind, string?, string, bool)[]
        {
            ("tad", "TAD prepared", GateItemKind.Deliverable, null, "SA", false),
            ("review", "Architecture reviewed", GateItemKind.Approval, null, "SA", false),
            ("security", "Security review completed", GateItemKind.Approval, null, "SA", false),
            ("signoff", "Customer signoff", GateItemKind.Signoff, null, "Customer", true),
        }),
        ("G6", "Delivery Readiness", 8, "Engineering-ready status", new (string, string, GateItemKind, string?, string, bool)[]
        {
            ("frozen", "Scope frozen", GateItemKind.Task, null, "SA", false),
            ("access", "Access available", GateItemKind.Prerequisite, null, "Customer", false),
            ("env", "Environments ready", GateItemKind.Prerequisite, null, "SA", false),
            ("rollback", "Rollback strategy defined", GateItemKind.Task, null, "SA", false),
            ("target", "Target platform selected", GateItemKind.Task, null, "SA", false),
        }),
        ("G7", "Delivery Governance", 7, "Progress reflected in systems · Issues escalated in time", new (string, string, GateItemKind, string?, string, bool)[]
        {
            ("mod.upgrade", "Version upgrade", GateItemKind.Task, "Modernization", "Engineer", false),
            ("mod.remediate", "Code remediation", GateItemKind.Task, "Modernization", "Engineer", false),
            ("mod.deps", "Dependency upgrade", GateItemKind.Task, "Modernization", "Engineer", false),
            ("mod.security", "Security fixes", GateItemKind.Task, "Modernization", "Engineer", false),
            ("cont.dockerfile", "Dockerfile", GateItemKind.Deliverable, "Containerization", "Engineer", false),
            ("cont.image", "Container image", GateItemKind.Deliverable, "Containerization", "Engineer", false),
            ("cont.registry", "Registry push", GateItemKind.Task, "Containerization", "Engineer", false),
            ("iac.bicep", "Bicep", GateItemKind.Deliverable, "IaC", "Engineer", false),
            ("iac.tf", "Terraform", GateItemKind.Deliverable, "IaC", "Engineer", false),
            ("iac.helm", "Helm charts", GateItemKind.Deliverable, "IaC", "Engineer", false),
            ("iac.aks", "AKS manifests", GateItemKind.Deliverable, "IaC", "Engineer", false),
            ("cicd.build", "Build pipeline", GateItemKind.Task, "CI/CD", "Engineer", false),
            ("cicd.release", "Release pipeline", GateItemKind.Task, "CI/CD", "Engineer", false),
            ("cicd.validate", "Deployment validation", GateItemKind.Task, "CI/CD", "Engineer", false),
            ("dep.migrate", "Migrate to target", GateItemKind.Task, "Deployment", "Engineer", false),
            ("dep.smoke", "Smoke validation", GateItemKind.Task, "Deployment", "Engineer", false),
            ("gov.status", "Weekly status · FDO hygiene", GateItemKind.Task, "Governance", "SA", false),
        }),
        ("G8", "Closure", 5, "Customer signoff · FDO closure · Lessons learned", new (string, string, GateItemKind, string?, string, bool)[]
        {
            ("uat", "UAT completed", GateItemKind.Task, null, "SA", false),
            ("signoff", "Customer signoff", GateItemKind.Signoff, null, "Customer", true),
            ("docs", "Documentation delivered", GateItemKind.Deliverable, null, "SA", false),
            ("kt", "Knowledge transfer completed", GateItemKind.Task, null, "SA", false),
            ("report", "Closure report", GateItemKind.Deliverable, null, "SA", true),
        }),
    };

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.GateDefinitions.AnyAsync(ct)) return;

        var order = 1;
        foreach (var (key, name, weight, exit, items) in Template)
        {
            var gate = new GateDefinition { Key = key, Name = name, Weight = weight, ExitCriteria = exit, Order = order++, OwnerRole = "SA" };
            var iOrder = 1;
            foreach (var (ikey, label, kind, sub, role, mandatory) in items)
                gate.Items.Add(new GateItemDefinition { Key = ikey, Label = label, Kind = kind, SubStage = sub, ResponsibleRole = role, Mandatory = mandatory, Order = iOrder++ });
            db.GateDefinitions.Add(gate);
        }
        await db.SaveChangesAsync(ct);
    }
}
