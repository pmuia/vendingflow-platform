using InventoryService.Domain.Entities.InventoryModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.EntityConfigurations.InventoryModule;

public class MachineInventoryConfiguration : IEntityTypeConfiguration<MachineInventory>
{
    public void Configure(EntityTypeBuilder<MachineInventory> builder)
    {
        builder.HasKey(inventory => inventory.Id);
        builder.Property(inventory => inventory.Id).ValueGeneratedNever();
        builder.HasIndex(inventory => new { inventory.MachineId, inventory.SlotNumber }).IsUnique();
        builder.HasOne(inventory => inventory.Product).WithMany().HasForeignKey(inventory => inventory.ProductId);
    }
}
