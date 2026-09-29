using InventoryService.Domain.Entities.InventoryModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.EntityConfigurations.InventoryModule;

public class VendingTransactionConfiguration : IEntityTypeConfiguration<VendingTransaction>
{
    public void Configure(EntityTypeBuilder<VendingTransaction> builder)
    {
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).ValueGeneratedNever();
        builder.HasIndex(transaction => transaction.TransactionId).IsUnique();
        builder.Property(transaction => transaction.Amount).HasPrecision(12, 2);
        builder.Property(transaction => transaction.Status).HasConversion<string>().HasMaxLength(32);
    }
}
