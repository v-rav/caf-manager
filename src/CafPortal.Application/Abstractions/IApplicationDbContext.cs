using CafPortal.Domain.Entities;
using CafPortal.Domain.Entities.Configuration;
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

    DbSet<RegionConfiguration> Regions { get; }
    DbSet<RoleConfiguration> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<SegmentConfiguration> Segments { get; }
    DbSet<StrategicAccountConfiguration> StrategicAccountConfigurations { get; }
    DbSet<CapacityConfiguration> CapacityConfigurations { get; }
    DbSet<ApplicationSetting> ApplicationSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
