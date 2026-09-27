using CafPortal.Domain.Entities;
using CafPortal.Domain.Entities.Auth;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Abstractions;

/// <summary>Abstraction over the EF Core context so Application services stay persistence-agnostic.</summary>
public interface IApplicationDbContext
{
    DbSet<Resource> Resources { get; }
    DbSet<Account> Accounts { get; }
    DbSet<AccountAlias> AccountAliases { get; }
    DbSet<ResourceAccount> ResourceAccounts { get; }
    DbSet<LeaveFact> LeaveFacts { get; }
    DbSet<EngagementFact> EngagementFacts { get; }
    DbSet<CapacityFact> CapacityFacts { get; }
    DbSet<Nomination> Nominations { get; }
    DbSet<WaveLink> WaveLinks { get; }
    DbSet<NominationResource> NominationResources { get; }
    DbSet<OwnershipHistory> OwnershipHistory { get; }
    DbSet<PerformanceReview> PerformanceReviews { get; }
    DbSet<ImportRun> ImportRuns { get; }
    DbSet<ImportChange> ImportChanges { get; }

    DbSet<RegionConfiguration> Regions { get; }
    DbSet<RoleConfiguration> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<SegmentConfiguration> Segments { get; }
    DbSet<LookupValue> LookupValues { get; }
    DbSet<ToolConfiguration> Tools { get; }
    DbSet<SkillConfiguration> Skills { get; }
    DbSet<StrategicAccountConfiguration> StrategicAccountConfigurations { get; }
    DbSet<CapacityConfiguration> CapacityConfigurations { get; }
    DbSet<ApplicationSetting> ApplicationSettings { get; }
    DbSet<AppUser> AppUsers { get; }
    DbSet<GateDefinition> GateDefinitions { get; }
    DbSet<GateItemDefinition> GateItemDefinitions { get; }
    DbSet<NominationGateItem> NominationGateItems { get; }
    DbSet<NominationBlocker> NominationBlockers { get; }
    DbSet<NominationEvent> NominationEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
