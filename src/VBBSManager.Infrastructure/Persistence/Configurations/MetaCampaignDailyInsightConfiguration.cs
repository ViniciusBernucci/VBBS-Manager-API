using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class MetaCampaignDailyInsightConfiguration : IEntityTypeConfiguration<MetaCampaignDailyInsight>
{
    public void Configure(EntityTypeBuilder<MetaCampaignDailyInsight> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CampaignId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CampaignName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Date).HasColumnType("date");
        builder.Property(x => x.Spend).HasPrecision(18, 2);
        builder.Property(x => x.Revenue).HasPrecision(18, 2);
        builder.Property(x => x.Cpc).HasPrecision(18, 4);
        builder.Property(x => x.Cpm).HasPrecision(18, 4);
        builder.Property(x => x.Ctr).HasPrecision(10, 4);
        builder.Property(x => x.Frequency).HasPrecision(10, 4);

        builder.HasIndex(x => new { x.TenantId, x.CampaignId, x.Date }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Date });
    }
}
