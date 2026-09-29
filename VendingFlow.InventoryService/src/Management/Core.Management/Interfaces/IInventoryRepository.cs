using InventoryService.Domain.Entities.InventoryModule;

namespace InventoryService.Application.Interfaces;

public interface IInventoryRepository
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct);
    Task<IReadOnlyList<MachineInventory>> GetInventoryAsync(string machineId, CancellationToken ct);
    Task<MachineInventory?> GetSlotAsync(string machineId, Guid productId, CancellationToken ct);
    Task<VendingTransaction?> GetTransactionAsync(string transactionId, CancellationToken ct);
    Task<IReadOnlyList<VendingTransaction>> RecentTransactionsAsync(CancellationToken ct);
    Task AddTransactionAsync(VendingTransaction transaction, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
