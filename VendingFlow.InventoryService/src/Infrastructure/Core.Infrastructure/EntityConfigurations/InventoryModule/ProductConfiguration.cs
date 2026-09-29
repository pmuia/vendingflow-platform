using InventoryService.Domain.Entities.InventoryModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryService.Infrastructure.EntityConfigurations.InventoryModule;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.HasIndex(product => product.ProductCode).IsUnique();
        builder.Property(product => product.Price).HasPrecision(12, 2);
    }
}
