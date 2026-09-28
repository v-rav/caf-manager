using CafPortal.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafPortal.Infrastructure.Persistence.Configurations;

public class GateDefinitionMap : IEntityTypeConfiguration<GateDefinition>
{
    public void Configure(EntityTypeBuilder<GateDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ExitCriteria).HasMaxLength(500);
        builder.Property(x => x.OwnerRole).HasMaxLength(20);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasMany(x => x.Items).WithOne(i => i.Gate!).HasForeignKey(i => i.GateDefinitionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GateItemDefinitionMap : IEntityTypeConfiguration<GateItemDefinition>
{
    public void Configure(EntityTypeBuilder<GateItemDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.SubStage).HasMaxLength(40);
        builder.Property(x => x.ResponsibleRole).HasMaxLength(20);
        builder.HasIndex(x => new { x.GateDefinitionId, x.Key }).IsUnique();
    }
}

public class NominationGateItemMap : IEntityTypeConfiguration<NominationGateItem>
{
    public void Configure(EntityTypeBuilder<NominationGateItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Owner).HasMaxLength(120);
        builder.Property(x => x.Ref).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.UpdatedBy).HasMaxLength(120);
        builder.HasOne(x => x.ItemDefinition).WithMany().HasForeignKey(x => x.GateItemDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.NominationId, x.GateItemDefinitionId }).IsUnique();
    }
}

public class NominationBlockerMap : IEntityTypeConfiguration<NominationBlocker>
{
    public void Configure(EntityTypeBuilder<NominationBlocker> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Owner).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.RaisedBy).HasMaxLength(120);
        builder.Property(x => x.ResolvedBy).HasMaxLength(120);
        builder.HasIndex(x => new { x.NominationId, x.ResolvedUtc });
    }
}

public class NominationEventMap : IEntityTypeConfiguration<NominationEvent>
{
    public void Configure(EntityTypeBuilder<NominationEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Field).HasMaxLength(200);
        builder.Property(x => x.OldValue).HasMaxLength(500);
        builder.Property(x => x.NewValue).HasMaxLength(500);
        builder.Property(x => x.ByUser).HasMaxLength(120);
        builder.HasIndex(x => new { x.NominationId, x.AtUtc });
    }
}

public class NominationMilestoneMap : IEntityTypeConfiguration<NominationMilestone>
{
    public void Configure(EntityTypeBuilder<NominationMilestone> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MilestoneKey).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ToolUsed).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.RecordedBy).HasMaxLength(120);
        builder.HasIndex(x => new { x.NominationId, x.OccurredOn });
    }
}

public class NominationToolUsageMap : IEntityTypeConfiguration<NominationToolUsage>
{
    public void Configure(EntityTypeBuilder<NominationToolUsage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UsedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => x.NominationId);
        builder.HasIndex(x => x.ToolId);
        builder.HasIndex(x => x.ActivityId);
    }
}
