namespace InventoryService.Domain.Entities.InventoryModule;

public class MachineInventory
{
    public long Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int SlotNumber { get; set; }
    public int Quantity { get; set; }
    public int Capacity { get; set; }
    public int LowStockThreshold { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
