using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Entities.Auth;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountAlias> AccountAliases => Set<AccountAlias>();
    public DbSet<ParkedAccount> ParkedAccounts => Set<ParkedAccount>();
    public DbSet<ResourceAccount> ResourceAccounts => Set<ResourceAccount>();
    public DbSet<LeaveFact> LeaveFacts => Set<LeaveFact>();
    public DbSet<EngagementFact> EngagementFacts => Set<EngagementFact>();
    public DbSet<CapacityFact> CapacityFacts => Set<CapacityFact>();
    public DbSet<Nomination> Nominations => Set<Nomination>();
    public DbSet<WaveLink> WaveLinks => Set<WaveLink>();
    public DbSet<NominationResource> NominationResources => Set<NominationResource>();
    public DbSet<OwnershipHistory> OwnershipHistory => Set<OwnershipHistory>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();
    public DbSet<ImportChange> ImportChanges => Set<ImportChange>();

    public DbSet<RegionConfiguration> Regions => Set<RegionConfiguration>();
    public DbSet<RoleConfiguration> Roles => Set<RoleConfiguration>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SegmentConfiguration> Segments => Set<SegmentConfiguration>();
    public DbSet<LookupValue> LookupValues => Set<LookupValue>();
    public DbSet<ToolConfiguration> Tools => Set<ToolConfiguration>();
    public DbSet<SkillConfiguration> Skills => Set<SkillConfiguration>();
    public DbSet<StrategicAccountConfiguration> StrategicAccountConfigurations => Set<StrategicAccountConfiguration>();
    public DbSet<CapacityConfiguration> CapacityConfigurations => Set<CapacityConfiguration>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<GateDefinition> GateDefinitions => Set<GateDefinition>();
    public DbSet<GateItemDefinition> GateItemDefinitions => Set<GateItemDefinition>();
    public DbSet<NominationGateItem> NominationGateItems => Set<NominationGateItem>();
    public DbSet<NominationBlocker> NominationBlockers => Set<NominationBlocker>();
    public DbSet<NominationEvent> NominationEvents => Set<NominationEvent>();
    public DbSet<NominationMilestone> NominationMilestones => Set<NominationMilestone>();
    public DbSet<MigrationTool> MigrationTools => Set<MigrationTool>();
    public DbSet<MigrationActivity> MigrationActivities => Set<MigrationActivity>();
    public DbSet<MigrationToolActivity> MigrationToolActivities => Set<MigrationToolActivity>();
    public DbSet<NominationToolUsage> NominationToolUsages => Set<NominationToolUsage>();
    public DbSet<AcrRecoveryEntry> AcrRecoveryEntries => Set<AcrRecoveryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
