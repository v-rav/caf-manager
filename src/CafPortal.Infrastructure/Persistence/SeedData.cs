using CafPortal.Domain.Entities;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

/// <summary>
/// Seeds configuration tables (regions, roles, permissions, thresholds, aliases, strategic accounts)
/// and, when the database is otherwise empty, a small demo dataset so the dashboard is usable
/// before any source workbooks are supplied.
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
        => await SeedAsync(db, seedDemo: true, ct);

    public static async Task SeedAsync(AppDbContext db, bool seedDemo, CancellationToken ct = default)
    {
        await SeedRegionsAsync(db, ct);
        await SeedRolesAsync(db, ct);
        await SeedSegmentsAsync(db, ct);
        await SeedCapacityConfigAsync(db, ct);
        await SeedApplicationSettingsAsync(db, ct);
        await SeedAliasesAsync(db, ct);
        await SeedStrategicAccountsAsync(db, ct);
        await db.SaveChangesAsync(ct);

        // Demo data is only seeded when requested (no real source workbook) and the DB has no resources yet.
        if (seedDemo && !await db.Resources.AnyAsync(ct))
            await SeedDemoDataAsync(db, ct);
    }

    private static async Task SeedRegionsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Regions.AnyAsync(ct))
            return;
        db.Regions.AddRange(
            new RegionConfiguration { Code = "EMEA", DisplayName = "Europe, Middle East & Africa", SortOrder = 1 },
            new RegionConfiguration { Code = "ASIA", DisplayName = "Asia Pacific", SortOrder = 2 },
            new RegionConfiguration { Code = "AMER", DisplayName = "Americas", SortOrder = 3 });
    }

    private static async Task SeedRolesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(ct))
            return;

        string[] adminPerms = ["view.dashboard", "view.resources", "view.accounts", "view.capacity", "view.leave", "view.strategic", "view.nominations", "manage.configuration"];
        string[] leadPerms = ["view.dashboard", "view.resources", "view.accounts", "view.capacity", "view.leave", "view.strategic", "view.nominations"];
        string[] memberPerms = ["view.dashboard", "view.resources", "view.accounts", "view.capacity"];
        string[] viewerPerms = ["view.dashboard"];

        var roles = new (string Name, string Desc, int Sort, string[] Perms)[]
        {
            ("Global Lead", "Global CAF operations owner", 1, adminPerms),
            ("Regional Lead", "Regional operations owner", 2, leadPerms),
            ("Architect Lead", "Leads architecture practice", 3, leadPerms),
            ("App Architect", "Application architect", 4, memberPerms),
            ("App Engineer", "Application engineer", 5, memberPerms),
            ("DevOps Engineer", "DevOps engineer", 6, memberPerms),
            ("SME", "Subject matter expert", 7, memberPerms),
            ("Viewer", "Read-only executive viewer", 8, viewerPerms)
        };

        foreach (var (name, desc, sort, perms) in roles)
        {
            db.Roles.Add(new RoleConfiguration
            {
                RoleName = name,
                Description = desc,
                SortOrder = sort,
                Permissions = perms.Select(p => new RolePermission { PermissionKey = p, Granted = true }).ToList()
            });
        }
    }

    private static async Task SeedSegmentsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Segments.AnyAsync(ct))
            return;
        var names = new[] { "Majors Growth", "SME&C - Corporate", "SME&C - SMB", "Strategic", "Upper Majors" };
        for (var i = 0; i < names.Length; i++)
            db.Segments.Add(new SegmentConfiguration { Name = names[i], SortOrder = i + 1 });
    }

    private static async Task SeedCapacityConfigAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.CapacityConfigurations.AnyAsync(ct))
            return;
        db.CapacityConfigurations.AddRange(
            new CapacityConfiguration { RoleName = "App Architect", CapacityLimit = 5, WarningThreshold = 80, OverloadedThreshold = 100 },
            new CapacityConfiguration { RoleName = "App Engineer", CapacityLimit = 5, WarningThreshold = 80, OverloadedThreshold = 100 },
            new CapacityConfiguration { RoleName = "DevOps Engineer", CapacityLimit = 5, WarningThreshold = 80, OverloadedThreshold = 100 },
            new CapacityConfiguration { RoleName = "Architect Lead", CapacityLimit = 8, WarningThreshold = 80, OverloadedThreshold = 100 });
    }

    private static async Task SeedApplicationSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.ApplicationSettings.AnyAsync(ct))
            return;
        db.ApplicationSettings.AddRange(
            new ApplicationSetting { Key = "DefaultCapacityLimit", Value = "5", Description = "Optimal active accounts per resource" },
            new ApplicationSetting { Key = "RefreshTime", Value = "02:00", Description = "Daily background refresh time (local)" },
            new ApplicationSetting { Key = "AvailableThreshold", Value = "40", Description = "Utilization %% ceiling for Available" },
            new ApplicationSetting { Key = "PartiallyUtilizedThreshold", Value = "80", Description = "Utilization %% ceiling for Partially Utilized" },
            new ApplicationSetting { Key = "OverloadedThreshold", Value = "100", Description = "Utilization %% above which resource is Overloaded" });
    }

    private static async Task SeedAliasesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.AccountAliases.AnyAsync(ct))
            return;
        db.AccountAliases.AddRange(
            new AccountAlias { Alias = "SG", StandardAccountName = "Societe Generale" },
            new AccountAlias { Alias = "Soc Gen", StandardAccountName = "Societe Generale" },
            new AccountAlias { Alias = "TSN", StandardAccountName = "Skills Network" },
            new AccountAlias { Alias = "MB", StandardAccountName = "Mercedes-Benz" },
            new AccountAlias { Alias = "KPC", StandardAccountName = "Kuwait Petroleum Corporation" });
    }

    private static readonly string[] StrategicSeed =
    [
        "Air France", "SITA", "Societe Generale", "KOC Holding", "Skills Network",
        "Mercedes-Benz", "MOE", "KPC", "Glencore", "Simployer", "FNZ", "CAIT",
        "Repsol", "Swiss Re", "Bilfinger"
    ];

    private static async Task SeedStrategicAccountsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.StrategicAccountConfigurations.AnyAsync(ct))
            return;
        foreach (var name in StrategicSeed)
        {
            db.StrategicAccountConfigurations.Add(new StrategicAccountConfiguration
            {
                AccountName = name,
                StrategicFlag = true,
                PriorityWeight = 2,
                ExecutiveVisibilityFlag = true
            });
        }
    }

    private static async Task SeedDemoDataAsync(AppDbContext db, CancellationToken ct)
    {
        // Strategic accounts become real Account rows (Phase 1 = EMEA) flagged strategic.
        var accounts = StrategicSeed
            .Select(n => new Account { AccountName = n, Region = "EMEA", Status = "Active", StrategicFlag = true, PriorityWeight = 2 })
            .ToList();
        // A few non-strategic accounts across regions for balance.
        accounts.AddRange(
        [
            new Account { AccountName = "Contoso Bank", Region = "AMER", Status = "Active", PriorityWeight = 1 },
            new Account { AccountName = "Fabrikam Retail", Region = "ASIA", Status = "Active", PriorityWeight = 1 },
            new Account { AccountName = "Northwind Traders", Region = "AMER", Status = "Active", PriorityWeight = 1 },
            new Account { AccountName = "Tailwind Logistics", Region = "ASIA", Status = "Active", PriorityWeight = 1 }
        ]);
        db.Accounts.AddRange(accounts);
        await db.SaveChangesAsync(ct);

        var byName = accounts.ToDictionary(a => a.AccountName, StringComparer.OrdinalIgnoreCase);

        var resources = new List<Resource>
        {
            new() { Psid = "P1001", Name = "Ravinder Rana", Email = "ravinder.rana@example.com", Region = "EMEA", Role = "Regional Lead", PrimarySkill = "Cloud Architecture", Skills = "Azure;Migration", ExperienceYears = 14, Status = "Active", DedicatedFlag = true, CapacityLimit = 8 },
            new() { Psid = "P1002", Name = "Arunkumar Azariah Koilraj", Email = "arun.koilraj@example.com", Region = "EMEA", Role = "Global Lead", PrimarySkill = "CAF Governance", Skills = "Strategy;Azure", ExperienceYears = 18, Status = "Active", DedicatedFlag = true, CapacityLimit = 8 },
            new() { Psid = "P1003", Name = "Amit Bengali", Email = "amit.bengali@example.com", Region = "ASIA", Role = "Regional Lead", PrimarySkill = "Cloud Architecture", Skills = "Azure;AKS", ExperienceYears = 15, Status = "Active", DedicatedFlag = true, CapacityLimit = 8 },
            new() { Psid = "P1004", Name = "Sofia Muller", Email = "sofia.muller@example.com", Region = "EMEA", Role = "App Architect", PrimarySkill = "App Modernization", Skills = ".NET;Containers", ExperienceYears = 10, Status = "Active" },
            new() { Psid = "P1005", Name = "Liam O'Brien", Email = "liam.obrien@example.com", Region = "EMEA", Role = "App Engineer", PrimarySkill = "Java", Skills = "Spring;AKS", ExperienceYears = 7, Status = "Active" },
            new() { Psid = "P1006", Name = "Noah Schmidt", Email = "noah.schmidt@example.com", Region = "EMEA", Role = "DevOps Engineer", PrimarySkill = "DevOps", Skills = "GitHub Actions;Bicep", ExperienceYears = 8, Status = "Active" },
            new() { Psid = "P1007", Name = "Priya Nair", Email = "priya.nair@example.com", Region = "ASIA", Role = "App Architect", PrimarySkill = "Data Platform", Skills = "Fabric;SQL", ExperienceYears = 11, Status = "Active" },
            new() { Psid = "P1008", Name = "Chen Wei", Email = "chen.wei@example.com", Region = "ASIA", Role = "App Engineer", PrimarySkill = "Node.js", Skills = "React;Azure", ExperienceYears = 6, Status = "Active" },
            new() { Psid = "P1009", Name = "Maria Garcia", Email = "maria.garcia@example.com", Region = "AMER", Role = "App Architect", PrimarySkill = "Cloud Native", Skills = "Kubernetes;Go", ExperienceYears = 12, Status = "Active" },
            new() { Psid = "P1010", Name = "James Patel", Email = "james.patel@example.com", Region = "AMER", Role = "DevOps Engineer", PrimarySkill = "Platform Engineering", Skills = "Terraform;Azure", ExperienceYears = 9, Status = "Active" },
            new() { Psid = "P1011", Name = "Elena Rossi", Email = "elena.rossi@example.com", Region = "EMEA", Role = "SME", PrimarySkill = "Security", Skills = "Entra;Defender", ExperienceYears = 13, Status = "Active" },
            new() { Psid = "P1012", Name = "David Kim", Email = "david.kim@example.com", Region = "AMER", Role = "App Engineer", PrimarySkill = "Python", Skills = "FastAPI;Azure", ExperienceYears = 5, Status = "Active" }
        };
        foreach (var r in resources.Where(r => r.CapacityLimit <= 0))
            r.CapacityLimit = 5;
        db.Resources.AddRange(resources);
        await db.SaveChangesAsync(ct);

        // Deterministic-ish assignments to create a spread of utilization bands.
        var assignments = new (string Resource, string[] Accounts)[]
        {
            ("Sofia Muller", ["Air France", "SITA", "Societe Generale", "KOC Holding", "Skills Network", "Mercedes-Benz"]), // overloaded
            ("Liam O'Brien", ["Glencore", "Simployer", "FNZ", "CAIT"]), // partially/fully
            ("Noah Schmidt", ["Repsol", "Swiss Re", "Bilfinger"]),
            ("Priya Nair", ["Fabrikam Retail", "Tailwind Logistics"]),
            ("Chen Wei", ["Fabrikam Retail"]),
            ("Maria Garcia", ["Contoso Bank", "Northwind Traders", "MOE", "KPC", "Repsol"]), // fully utilized
            ("James Patel", ["Contoso Bank", "Northwind Traders"]),
            ("David Kim", ["Northwind Traders"]),
            ("Ravinder Rana", ["Air France", "Societe Generale"]),
            ("Elena Rossi", ["SITA", "Swiss Re", "Bilfinger"])
        };

        var resByName = resources.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var (resName, accNames) in assignments)
        {
            if (!resByName.TryGetValue(resName, out var res))
                continue;
            var i = 0;
            foreach (var accName in accNames)
            {
                if (!byName.TryGetValue(accName, out var acc))
                    continue;
                db.ResourceAccounts.Add(new ResourceAccount
                {
                    ResourceId = res.ResourceId,
                    AccountId = acc.AccountId,
                    Source = "Seed",
                    RelationshipType = i++ == 0 ? "Primary" : "Secondary"
                });
            }
        }

        // Upcoming leave for a few resources.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.LeaveFacts.AddRange(
            new LeaveFact { ResourceId = resByName["Liam O'Brien"].ResourceId, LeaveDate = today.AddDays(3), LeaveType = "Leave" },
            new LeaveFact { ResourceId = resByName["Liam O'Brien"].ResourceId, LeaveDate = today.AddDays(4), LeaveType = "Leave" },
            new LeaveFact { ResourceId = resByName["Chen Wei"].ResourceId, LeaveDate = today, LeaveType = "Holiday" },
            new LeaveFact { ResourceId = resByName["Maria Garcia"].ResourceId, LeaveDate = today.AddDays(20), LeaveType = "Leave" },
            new LeaveFact { ResourceId = resByName["David Kim"].ResourceId, LeaveDate = today.AddDays(45), LeaveType = "Special Leave" });

        // Recent engagement activity against strategic accounts.
        db.EngagementFacts.AddRange(
            new EngagementFact { Date = today.AddDays(-2), ResourceId = resByName["Sofia Muller"].ResourceId, AccountId = byName["Societe Generale"].AccountId, MeetingName = "Migration roadmap", Duration = 1.5, Region = "EMEA", Remarks = "Positive demand signal" },
            new EngagementFact { Date = today.AddDays(-5), ResourceId = resByName["Ravinder Rana"].ResourceId, AccountId = byName["Air France"].AccountId, MeetingName = "Executive review", Duration = 1, Region = "EMEA" },
            new EngagementFact { Date = today.AddDays(-1), ResourceId = resByName["Elena Rossi"].ResourceId, AccountId = byName["SITA"].AccountId, MeetingName = "Security assessment", Duration = 2, Region = "EMEA" },
            new EngagementFact { Date = today.AddDays(-10), ResourceId = resByName["Maria Garcia"].ResourceId, AccountId = byName["KPC"].AccountId, MeetingName = "Landing zone design", Duration = 2, Region = "AMER" });

        // Nomination pipeline.
        db.Nominations.AddRange(
            new Nomination { AccountId = byName["Glencore"].AccountId, AccountName = "Glencore", Technology = "AKS Migration", Region = "EMEA", Status = NominationStatusType.Open, OpenedDate = today.AddDays(-7), Remarks = "Awaiting architect" },
            new Nomination { AccountId = byName["FNZ"].AccountId, AccountName = "FNZ", Technology = "App Modernization", Region = "EMEA", Status = NominationStatusType.InProgress, OpenedDate = today.AddDays(-14) },
            new Nomination { AccountId = byName["Contoso Bank"].AccountId, AccountName = "Contoso Bank", Technology = "Data Platform", Region = "AMER", Status = NominationStatusType.Open, OpenedDate = today.AddDays(-3) },
            new Nomination { AccountId = byName["Fabrikam Retail"].AccountId, AccountName = "Fabrikam Retail", Technology = "DevOps Enablement", Region = "ASIA", Status = NominationStatusType.Closed, OpenedDate = today.AddDays(-40), Remarks = "Delivered" });

        await db.SaveChangesAsync(ct);
    }
}
