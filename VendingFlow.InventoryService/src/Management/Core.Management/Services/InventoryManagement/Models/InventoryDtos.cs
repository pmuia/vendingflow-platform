using InventoryService.Domain.Entities.InventoryModule;

namespace InventoryService.Application.Services.InventoryManagement.Models;

public record ProductDto(long Id, string ProductCode, string Name, decimal Price, string Currency);

public record InventoryDto(string MachineId, long ProductId, string ProductName, int SlotNumber, int Quantity, int Capacity, int LowStockThreshold);

public record TransactionDto(string TransactionId, string MachineId, long ProductId, int SlotNumber, int Quantity,
    decimal Amount, string Currency, string PaymentMethod, string CustomerPhone, string Status, string CorrelationId,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

public record ItemLedgerDto(long Id, string MachineId, long ProductId, string ProductName, string TransactionId,
    string MovementType, int QuantityChange, int BalanceAfter, decimal UnitPrice, decimal TotalAmount,
    string Currency, DateTimeOffset CreatedAt);

public static class InventoryMapping
{
    public static ProductDto ToDto(this Product product) => new(product.Id, product.ProductCode, product.Name, product.Price, product.Currency);

    public static InventoryDto ToDto(this MachineInventory item) => new(item.MachineId, item.ProductId, item.Product.Name, item.SlotNumber, item.Quantity, item.Capacity, item.LowStockThreshold);

    public static TransactionDto ToDto(this VendingTransaction transaction) => new(transaction.TransactionId,
        transaction.MachineId, transaction.ProductId, transaction.SlotNumber, transaction.Quantity,
        transaction.Amount, transaction.Currency, transaction.PaymentMethod, transaction.CustomerPhone,
        transaction.Status.ToString(), transaction.CorrelationId, transaction.CreatedAt, transaction.CompletedAt);

    public static ItemLedgerDto ToDto(this ItemLedgerEntry entry) => new(entry.Id, entry.MachineId,
        entry.ProductId, entry.Product.Name, entry.TransactionId, entry.MovementType.ToString(),
        entry.QuantityChange, entry.BalanceAfter, entry.UnitPrice, entry.TotalAmount, entry.Currency,
        entry.CreatedAt);
}
