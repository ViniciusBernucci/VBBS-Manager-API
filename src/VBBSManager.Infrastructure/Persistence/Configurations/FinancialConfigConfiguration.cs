using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class FinancialConfigConfiguration : IEntityTypeConfiguration<FinancialConfig>
{
    public void Configure(EntityTypeBuilder<FinancialConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.MonthlyGrossRevenue).HasPrecision(18, 2);
        builder.Property(c => c.MonthlyAdSpend).HasPrecision(18, 2);
        builder.Property(c => c.HotmartFeePercent).HasPrecision(5, 4);
        builder.Property(c => c.InstallmentFeePercent).HasPrecision(5, 4);
        builder.Property(c => c.InstallmentSalesPercent).HasPrecision(5, 4);
        builder.Property(c => c.FederalTaxPercent).HasPrecision(5, 4);
        builder.Property(c => c.RefundRatePercent).HasPrecision(5, 4);
        builder.Property(c => c.MetaAdsTaxPercent).HasPrecision(5, 4);
        builder.Property(c => c.AccountingCost).HasPrecision(18, 2);
        builder.Property(c => c.InvoicingCost).HasPrecision(18, 2);
        builder.Property(c => c.ManychatCost).HasPrecision(18, 2);
        builder.Property(c => c.HotmartPlayerCost).HasPrecision(18, 2);
        builder.Property(c => c.HotmartFixedFeePerTransaction).HasPrecision(10, 4);

        // Only one config per tenant
        builder.HasIndex(c => c.TenantId).IsUnique();
    }
}
