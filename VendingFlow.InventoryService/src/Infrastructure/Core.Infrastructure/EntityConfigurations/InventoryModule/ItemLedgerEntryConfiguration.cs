using InventoryService.Domain.Entities.InventoryModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.EntityConfigurations.InventoryModule;

public class ItemLedgerEntryConfiguration : IEntityTypeConfiguration<ItemLedgerEntry>
{
    public void Configure(EntityTypeBuilder<ItemLedgerEntry> builder)
    {
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.MovementType).HasConversion<string>().HasMaxLength(32);
        builder.Property(entry => entry.UnitPrice).HasPrecision(12, 2);
        builder.Property(entry => entry.TotalAmount).HasPrecision(12, 2);
        builder.Property(entry => entry.TransactionId).HasMaxLength(64);
        builder.HasIndex(entry => new { entry.MachineId, entry.CreatedAt });
        builder.HasIndex(entry => entry.TransactionId);
        builder.HasOne(entry => entry.Product).WithMany().HasForeignKey(entry => entry.ProductId);
    }
}
