using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using InventoryService.Domain.Entities.InventoryModule;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Commands;

[CommandName("reserveProduct")]
public sealed record ReserveProductCommand(string MachineId, Guid ProductId) : IRequest<ResponseModel>;

public sealed class ReserveProductCommandHandler(IInventoryRepository repository, IEventBus events) : IRequestHandler<ReserveProductCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(ReserveProductCommand request, CancellationToken cancellationToken)
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

        return ResponseModel.Ok(tx.ToDto(), "Vending transaction created");
    }
}
