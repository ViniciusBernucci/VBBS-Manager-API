using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class FixedExpenseConfiguration : IEntityTypeConfiguration<FixedExpense>
{
    public void Configure(EntityTypeBuilder<FixedExpense> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Amount).HasPrecision(18, 2);
        builder.HasIndex(f => new { f.TenantId, f.IsActive });
    }
}
