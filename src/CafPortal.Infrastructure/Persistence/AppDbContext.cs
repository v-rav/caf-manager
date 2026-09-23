using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountAlias> AccountAliases => Set<AccountAlias>();
    public DbSet<ResourceAccount> ResourceAccounts => Set<ResourceAccount>();
    public DbSet<LeaveFact> LeaveFacts => Set<LeaveFact>();
    public DbSet<EngagementFact> EngagementFacts => Set<EngagementFact>();
    public DbSet<CapacityFact> CapacityFacts => Set<CapacityFact>();
    public DbSet<Nomination> Nominations => Set<Nomination>();

    public DbSet<RegionConfiguration> Regions => Set<RegionConfiguration>();
    public DbSet<RoleConfiguration> Roles => Set<RoleConfiguration>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SegmentConfiguration> Segments => Set<SegmentConfiguration>();
    public DbSet<StrategicAccountConfiguration> StrategicAccountConfigurations => Set<StrategicAccountConfiguration>();
    public DbSet<CapacityConfiguration> CapacityConfigurations => Set<CapacityConfiguration>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
