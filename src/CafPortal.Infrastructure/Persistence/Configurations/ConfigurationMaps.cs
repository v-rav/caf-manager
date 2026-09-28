using CafPortal.Domain.Entities.Auth;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafPortal.Infrastructure.Persistence.Configurations;

public class RegionConfigurationMap : IEntityTypeConfiguration<RegionConfiguration>
{
    public void Configure(EntityTypeBuilder<RegionConfiguration> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class RoleConfigurationMap : IEntityTypeConfiguration<RoleConfiguration>
{
    public void Configure(EntityTypeBuilder<RoleConfiguration> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.HasIndex(x => x.RoleName).IsUnique();
        builder.HasMany(x => x.Permissions).WithOne(p => p.Role)
            .HasForeignKey(p => p.RoleConfigurationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SegmentConfigurationMap : IEntityTypeConfiguration<SegmentConfiguration>
{
    public void Configure(EntityTypeBuilder<SegmentConfiguration> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class LookupValueMap : IEntityTypeConfiguration<LookupValue>
{
    public void Configure(EntityTypeBuilder<LookupValue> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.Category, x.Value }).IsUnique();
    }
}

public class ToolConfigurationMap : IEntityTypeConfiguration<ToolConfiguration>
{
    public void Configure(EntityTypeBuilder<ToolConfiguration> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class SkillConfigurationMap : IEntityTypeConfiguration<SkillConfiguration>
{
    public void Configure(EntityTypeBuilder<SkillConfiguration> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class MigrationToolMap : IEntityTypeConfiguration<MigrationTool>
{
    public void Configure(EntityTypeBuilder<MigrationTool> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Vendor).HasMaxLength(80);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class MigrationActivityMap : IEntityTypeConfiguration<MigrationActivity>
{
    public void Configure(EntityTypeBuilder<MigrationActivity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Stage).HasMaxLength(40);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class MigrationToolActivityMap : IEntityTypeConfiguration<MigrationToolActivity>
{
    public void Configure(EntityTypeBuilder<MigrationToolActivity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ToolId, x.ActivityId }).IsUnique();
        builder.HasIndex(x => x.ToolId);
    }
}

public class RolePermissionMap : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PermissionKey).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.RoleConfigurationId, x.PermissionKey }).IsUnique();
    }
}

public class StrategicAccountConfigurationMap : IEntityTypeConfiguration<StrategicAccountConfiguration>
{
    public void Configure(EntityTypeBuilder<StrategicAccountConfiguration> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.AccountName);
        builder.HasOne(x => x.Account).WithMany()
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CapacityConfigurationMap : IEntityTypeConfiguration<CapacityConfiguration>
{
    public void Configure(EntityTypeBuilder<CapacityConfiguration> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.RoleName).IsUnique();
    }
}

public class ApplicationSettingMap : IEntityTypeConfiguration<ApplicationSetting>
{
    public void Configure(EntityTypeBuilder<ApplicationSetting> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public class AppUserMap : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Role).HasConversion<int>();
        builder.HasIndex(x => x.Username).IsUnique();
    }
}
