using CafPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafPortal.Infrastructure.Persistence.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.HasKey(r => r.ResourceId);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Psid).HasMaxLength(50);
        builder.Property(r => r.Email).HasMaxLength(256);
        builder.Property(r => r.Mobile).HasMaxLength(40);
        builder.Property(r => r.Aliases).HasMaxLength(400);
        builder.Property(r => r.Region).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Role).HasMaxLength(100).IsRequired();
        builder.Property(r => r.PrimarySkill).HasMaxLength(200);
        builder.Property(r => r.Status).HasMaxLength(50);
        builder.Property(r => r.OnboardingStatus).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(r => r.Region);
        builder.HasIndex(r => r.Psid);
        // Email is the stable identity: unique when present, blanks excluded.
        builder.HasIndex(r => r.Email).IsUnique().HasFilter("[Email] IS NOT NULL AND [Email] <> ''");
    }
}

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Region).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Status).HasMaxLength(50);
        builder.Property(a => a.Tpid).HasMaxLength(50);
        builder.Property(a => a.ExternalAccountId).HasMaxLength(50);
        builder.Property(a => a.Aliases).HasMaxLength(400);
        builder.Property(a => a.Segment).HasMaxLength(100);
        builder.Property(a => a.ProjectManager).HasMaxLength(200);
        builder.Property(a => a.SolutionArchitect).HasMaxLength(200);
        builder.Property(a => a.Cftl).HasMaxLength(200);
        builder.Property(a => a.AccountOwner).HasMaxLength(200);
        builder.Property(a => a.CustomerPoc).HasMaxLength(200);
        builder.Property(a => a.BackupOwner).HasMaxLength(200);
        builder.HasIndex(a => a.AccountName);
        builder.HasIndex(a => a.Region);
        builder.HasIndex(a => a.Tpid);
    }
}

public class AccountAliasConfiguration : IEntityTypeConfiguration<AccountAlias>
{
    public void Configure(EntityTypeBuilder<AccountAlias> builder)
    {
        builder.HasKey(a => a.AliasId);
        builder.Property(a => a.Alias).HasMaxLength(200).IsRequired();
        builder.Property(a => a.StandardAccountName).HasMaxLength(200).IsRequired();
        builder.HasIndex(a => a.Alias).IsUnique();
    }
}

public class ResourceAccountConfiguration : IEntityTypeConfiguration<ResourceAccount>
{
    public void Configure(EntityTypeBuilder<ResourceAccount> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Source).HasMaxLength(100);
        builder.Property(x => x.RelationshipType).HasMaxLength(50);
        builder.HasOne(x => x.Resource).WithMany(r => r.ResourceAccounts)
            .HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Account).WithMany(a => a.ResourceAccounts)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ResourceId, x.AccountId }).IsUnique();
    }
}

public class NominationResourceConfiguration : IEntityTypeConfiguration<NominationResource>
{
    public void Configure(EntityTypeBuilder<NominationResource> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasMaxLength(50);
        builder.Property(x => x.Source).HasMaxLength(20);
        builder.HasOne(x => x.Nomination).WithMany(n => n.ResourceAssignments)
            .HasForeignKey(x => x.NominationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Resource).WithMany()
            .HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.NominationId, x.ResourceId }).IsUnique();
    }
}
