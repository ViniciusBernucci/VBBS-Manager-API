using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class DailyAdSpendSummaryConfiguration : IEntityTypeConfiguration<DailyAdSpendSummary>
{
    public void Configure(EntityTypeBuilder<DailyAdSpendSummary> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Date).HasColumnType("date");
        builder.Property(x => x.TotalSpend).HasPrecision(18, 2);
        builder.Property(x => x.TotalRevenue).HasPrecision(18, 2);

        // Um registro por tenant por dia (agrega todas as campanhas)
        builder.HasIndex(x => new { x.TenantId, x.Date }).IsUnique();
    }
}
