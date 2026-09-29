using InventoryService.Domain;
using MediatR;

namespace InventoryService.Application;

public record ProductDto(Guid Id, string ProductCode, string Name, decimal Price, string Currency);
public record InventoryDto(string MachineId, Guid ProductId, string ProductName, int SlotNumber, int Quantity, int Capacity, int LowStockThreshold);
public record TransactionDto(string TransactionId, string MachineId, Guid ProductId, int SlotNumber, decimal Amount, string Currency, string Status, string CorrelationId);

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

public interface IEventBus
{
    Task PublishAsync(string eventType, string correlationId, string machineId, object payload, CancellationToken ct);
}

public record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;
public record GetMachineInventoryQuery(string MachineId) : IRequest<IReadOnlyList<InventoryDto>>;
public record GetRecentTransactionsQuery : IRequest<IReadOnlyList<TransactionDto>>;
public record StockMachineCommand(string MachineId, Guid ProductId, int Quantity) : IRequest<InventoryDto>;
public record ReserveProductCommand(string MachineId, Guid ProductId) : IRequest<TransactionDto>;

public static class InventoryMapping
{
    public static ProductDto ToDto(this Product product) => new(product.Id, product.ProductCode, product.Name, product.Price, product.Currency);
    public static InventoryDto ToDto(this MachineInventory item) => new(item.MachineId, item.ProductId, item.Product.Name, item.SlotNumber, item.Quantity, item.Capacity, item.LowStockThreshold);
    public static TransactionDto ToDto(this VendingTransaction tx) => new(tx.TransactionId, tx.MachineId, tx.ProductId, tx.SlotNumber, tx.Amount, tx.Currency, tx.Status.ToString(), tx.CorrelationId);
}

public sealed class GetProductsHandler(IInventoryRepository repository) : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken) =>
        (await repository.GetProductsAsync(cancellationToken)).Select(p => p.ToDto()).ToList();
}

public sealed class GetMachineInventoryHandler(IInventoryRepository repository) : IRequestHandler<GetMachineInventoryQuery, IReadOnlyList<InventoryDto>>
{
    public async Task<IReadOnlyList<InventoryDto>> Handle(GetMachineInventoryQuery request, CancellationToken cancellationToken) =>
        (await repository.GetInventoryAsync(request.MachineId, cancellationToken)).Select(i => i.ToDto()).ToList();
}

public sealed class GetRecentTransactionsHandler(IInventoryRepository repository) : IRequestHandler<GetRecentTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    public async Task<IReadOnlyList<TransactionDto>> Handle(GetRecentTransactionsQuery request, CancellationToken cancellationToken) =>
        (await repository.RecentTransactionsAsync(cancellationToken)).Select(t => t.ToDto()).ToList();
}

public sealed class StockMachineHandler(IInventoryRepository repository, IEventBus events) : IRequestHandler<StockMachineCommand, InventoryDto>
{
    public async Task<InventoryDto> Handle(StockMachineCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetSlotAsync(request.MachineId, request.ProductId, cancellationToken) ?? throw new InvalidOperationException("Slot not found.");
        slot.Quantity = Math.Min(slot.Capacity, slot.Quantity + request.Quantity);
        slot.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        await events.PublishAsync("inventory.updated", Guid.NewGuid().ToString("N"), request.MachineId, slot.ToDto(), cancellationToken);
        return slot.ToDto();
    }
}

public sealed class ReserveProductHandler(IInventoryRepository repository, IEventBus events) : IRequestHandler<ReserveProductCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(ReserveProductCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetSlotAsync(request.MachineId, request.ProductId, cancellationToken) ?? throw new InvalidOperationException("Product unavailable for this machine.");
        if (slot.Quantity <= 0) throw new InvalidOperationException("Product is out of stock.");

        var tx = new VendingTransaction
        {
            Id = Guid.NewGuid(),
            TransactionId = $"VTX-{Guid.NewGuid():N}",
            MachineId = request.MachineId,
            ProductId = request.ProductId,
            SlotNumber = slot.SlotNumber,
            Amount = slot.Product.Price,
            Currency = slot.Product.Currency,
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow
        };
        tx.MoveTo(VendingTransactionStatus.AWAITING_PAYMENT);
        await repository.AddTransactionAsync(tx, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await events.PublishAsync("payment.requested", tx.CorrelationId, tx.MachineId, new
        {
            tx.TransactionId,
            tx.MachineId,
            tx.ProductId,
            tx.SlotNumber,
            tx.Amount,
            tx.Currency
        }, cancellationToken);
        return tx.ToDto();
    }
}
