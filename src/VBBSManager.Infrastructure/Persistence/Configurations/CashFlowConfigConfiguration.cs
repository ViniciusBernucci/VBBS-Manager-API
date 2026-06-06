using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class CashFlowConfigConfiguration : IEntityTypeConfiguration<CashFlowConfig>
{
    public void Configure(EntityTypeBuilder<CashFlowConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.InitialBalance).HasPrecision(18, 2);
        builder.HasIndex(c => c.TenantId).IsUnique();
    }
}
