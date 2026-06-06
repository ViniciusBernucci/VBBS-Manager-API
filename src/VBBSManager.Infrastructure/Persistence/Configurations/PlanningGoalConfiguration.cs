using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class PlanningGoalConfiguration : IEntityTypeConfiguration<PlanningGoal>
{
    public void Configure(EntityTypeBuilder<PlanningGoal> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Key).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(500);
        builder.Property(g => g.TargetValue).HasPrecision(18, 4);
        builder.Property(g => g.CurrentValue).HasPrecision(18, 4);
        builder.Property(g => g.Unit).HasMaxLength(20).IsRequired();
        builder.Property(g => g.ActionIfFailed).HasMaxLength(500);
        builder.Property(g => g.Category).HasConversion<string>();
        builder.Property(g => g.ComparisonType).HasConversion<string>();

        builder.HasIndex(g => new { g.TenantId, g.Key }).IsUnique();
        builder.HasIndex(g => g.TenantId);
    }
}
