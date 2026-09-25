using CafPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafPortal.Infrastructure.Persistence.Configurations;

public class LeaveFactConfiguration : IEntityTypeConfiguration<LeaveFact>
{
    public void Configure(EntityTypeBuilder<LeaveFact> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LeaveType).HasMaxLength(100).IsRequired();
        builder.HasOne(x => x.Resource).WithMany(r => r.LeaveFacts)
            .HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.LeaveDate);
        builder.HasIndex(x => new { x.ResourceId, x.LeaveDate });
    }
}

public class EngagementFactConfiguration : IEntityTypeConfiguration<EngagementFact>
{
    public void Configure(EntityTypeBuilder<EngagementFact> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MeetingName).HasMaxLength(300);
        builder.Property(x => x.Region).HasMaxLength(50);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.HasOne(x => x.Resource).WithMany(r => r.EngagementFacts)
            .HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Account).WithMany(a => a.EngagementFacts)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.AccountId);
    }
}

public class CapacityFactConfiguration : IEntityTypeConfiguration<CapacityFact>
{
    public void Configure(EntityTypeBuilder<CapacityFact> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CapacityStatus).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(x => x.Resource).WithMany()
            .HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ResourceId);
    }
}

public class NominationConfiguration : IEntityTypeConfiguration<Nomination>
{
    public void Configure(EntityTypeBuilder<Nomination> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountName).HasMaxLength(200);
        builder.Property(x => x.ExternalTaskId).HasMaxLength(100);
        builder.Property(x => x.Technology).HasMaxLength(200);
        builder.Property(x => x.Region).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.MigrationStatus).HasMaxLength(100);
        builder.Property(x => x.ApprovalStatus).HasMaxLength(60);
        builder.Property(x => x.CurrentState).HasMaxLength(2000);
        builder.Property(x => x.SolutionArchitect).HasMaxLength(200);
        builder.Property(x => x.CftlPrimary).HasMaxLength(200);
        builder.Property(x => x.ProjectCoordinator).HasMaxLength(200);
        builder.Property(x => x.BlockedReason).HasConversion<string>().HasMaxLength(40);
        builder.HasOne(x => x.Account).WithMany()
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(x => x.WaveLinks).WithOne(w => w.Nomination)
            .HasForeignKey(w => w.NominationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExternalTaskId);
    }
}

public class WaveLinkConfiguration : IEntityTypeConfiguration<WaveLink>
{
    public void Configure(EntityTypeBuilder<WaveLink> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WaveType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Reference).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Source).HasMaxLength(20);
        builder.HasOne(x => x.Account).WithMany()
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.NominationId);
    }
}

public class OwnershipHistoryConfiguration : IEntityTypeConfiguration<OwnershipHistory>
{
    public void Configure(EntityTypeBuilder<OwnershipHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PreviousOwner).HasMaxLength(200);
        builder.Property(x => x.NewOwner).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasOne(x => x.Account).WithMany(a => a.OwnershipHistory)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.AccountId);
    }
}

public class PerformanceReviewConfiguration : IEntityTypeConfiguration<PerformanceReview>
{
    public void Configure(EntityTypeBuilder<PerformanceReview> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PersonName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(200);
        builder.Property(x => x.ReportingManager).HasMaxLength(200);
        builder.Property(x => x.Region).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Comments).HasMaxLength(2000);
        builder.HasIndex(x => new { x.PersonName, x.ReviewDate });
        builder.HasIndex(x => x.Region);
    }
}
