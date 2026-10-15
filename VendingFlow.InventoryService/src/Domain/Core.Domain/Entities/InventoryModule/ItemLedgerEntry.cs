namespace InventoryService.Domain.Entities.InventoryModule;

public class ItemLedgerEntry
{
    public long Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string TransactionId { get; set; } = string.Empty;
    public InventoryMovementType MovementType { get; set; }
    public int QuantityChange { get; set; }
    public int BalanceAfter { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "KES";
    public DateTimeOffset CreatedAt { get; set; }
}
