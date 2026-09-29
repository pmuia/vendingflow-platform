namespace InventoryService.Domain.Entities.InventoryModule;

public sealed class MachineInventory
{
    public Guid Id { get; set; }
    public string MachineId { get; set; } = "";
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int SlotNumber { get; set; }
    public int Quantity { get; set; }
    public int Capacity { get; set; }
    public int LowStockThreshold { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
