using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class CashFlowTransactionConfiguration : IEntityTypeConfiguration<CashFlowTransaction>
{
    public void Configure(EntityTypeBuilder<CashFlowTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Description).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Date).HasColumnType("date");
        builder.HasIndex(t => new { t.TenantId, t.Date });
    }
}
