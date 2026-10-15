using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using InventoryService.Domain.Entities.InventoryModule;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Commands;

[CommandName("stockMachine")]
public record StockMachineCommand(string MachineId, long ProductId, int Quantity) : IRequest<ResponseModel>;

public class StockMachineCommandHandler(IInventoryRepository repository, IEventBus events) : IRequestHandler<StockMachineCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(StockMachineCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetSlotAsync(request.MachineId, request.ProductId, cancellationToken) ?? throw new InvalidOperationException("Slot not found.");
        if (request.Quantity <= 0) throw new InvalidOperationException("Quantity must be at least one.");
        var before = slot.Quantity;
        slot.Quantity = Math.Min(slot.Capacity, slot.Quantity + request.Quantity);
        slot.UpdatedAt = DateTimeOffset.UtcNow;

        var quantityAdded = slot.Quantity - before;
        if (quantityAdded == 0) throw new InvalidOperationException("The slot is already at capacity.");

        await repository.AddLedgerEntryAsync(new ItemLedgerEntry
        {
            Id = LongIdGenerator.NextId(),
            MachineId = request.MachineId,
            ProductId = request.ProductId,
            Product = slot.Product,
            TransactionId = $"STOCK-{Guid.NewGuid():N}",
            MovementType = InventoryMovementType.STOCK_IN,
            QuantityChange = quantityAdded,
            BalanceAfter = slot.Quantity,
            UnitPrice = slot.Product.Price,
            TotalAmount = slot.Product.Price * quantityAdded,
            Currency = slot.Product.Currency,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);
        await events.PublishAsync("inventory.updated", Guid.NewGuid().ToString("N"), request.MachineId, slot.ToDto(), cancellationToken);

        return ResponseModel.Ok(slot.ToDto(), "Machine stocked");
    }
}
