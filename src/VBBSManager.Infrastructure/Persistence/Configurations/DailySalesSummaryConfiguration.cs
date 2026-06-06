using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class DailySalesSummaryConfiguration : IEntityTypeConfiguration<DailySalesSummary>
{
    public void Configure(EntityTypeBuilder<DailySalesSummary> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Date).HasColumnType("date");
        builder.Property(x => x.GrossRevenue).HasPrecision(18, 2);
        builder.Property(x => x.NetRevenue).HasPrecision(18, 2);
        builder.Property(x => x.HotmartFeeAmount).HasPrecision(18, 2);

        // Um registro por tenant por dia
        builder.HasIndex(x => new { x.TenantId, x.Date }).IsUnique();
    }
}
