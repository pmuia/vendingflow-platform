using InventoryService.Domain.Entities.InventoryModule;

namespace InventoryService.Application.Services.InventoryManagement.Models;

public record ProductDto(Guid Id, string ProductCode, string Name, decimal Price, string Currency);

public record InventoryDto(string MachineId, Guid ProductId, string ProductName, int SlotNumber, int Quantity, int Capacity, int LowStockThreshold);

public record TransactionDto(string TransactionId, string MachineId, Guid ProductId, int SlotNumber, decimal Amount, string Currency, string Status, string CorrelationId);

public static class InventoryMapping
{
    public static ProductDto ToDto(this Product product) => new(product.Id, product.ProductCode, product.Name, product.Price, product.Currency);

    public static InventoryDto ToDto(this MachineInventory item) => new(item.MachineId, item.ProductId, item.Product.Name, item.SlotNumber, item.Quantity, item.Capacity, item.LowStockThreshold);

    public static TransactionDto ToDto(this VendingTransaction transaction) => new(transaction.TransactionId, transaction.MachineId, transaction.ProductId, transaction.SlotNumber, transaction.Amount, transaction.Currency, transaction.Status.ToString(), transaction.CorrelationId);
}
