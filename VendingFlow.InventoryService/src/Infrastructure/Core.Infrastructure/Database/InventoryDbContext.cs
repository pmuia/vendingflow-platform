using System.Reflection;

namespace InventoryService.Infrastructure.Database;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<MachineInventory> MachineInventories => Set<MachineInventory>();
    public DbSet<VendingTransaction> VendingTransactions => Set<VendingTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventoryservice");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
