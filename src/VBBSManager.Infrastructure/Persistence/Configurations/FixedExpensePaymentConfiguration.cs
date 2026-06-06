using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class FixedExpensePaymentConfiguration : IEntityTypeConfiguration<FixedExpensePayment>
{
    public void Configure(EntityTypeBuilder<FixedExpensePayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.FixedExpense)
            .WithMany()
            .HasForeignKey(p => p.FixedExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CashFlowTransaction)
            .WithMany()
            .HasForeignKey(p => p.CashFlowTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // One payment per expense per month per tenant
        builder.HasIndex(p => new { p.TenantId, p.FixedExpenseId, p.Year, p.Month }).IsUnique();
    }
}
