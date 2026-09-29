using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using InventoryService.Domain.Entities.InventoryModule;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Commands;

[CommandName("reserveProduct")]
public record ReserveProductCommand(string MachineId, long ProductId) : IRequest<ResponseModel>;

public class ReserveProductCommandHandler(IInventoryRepository repository, IEventBus events) : IRequestHandler<ReserveProductCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(ReserveProductCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetSlotAsync(request.MachineId, request.ProductId, cancellationToken) ?? throw new InvalidOperationException("Product unavailable for this machine.");
        if (slot.Quantity <= 0) throw new InvalidOperationException("Product is out of stock.");

        var transaction = new VendingTransaction
        {
            Id = LongIdGenerator.NextId(),
            TransactionId = $"VTX-{Guid.NewGuid():N}",
            MachineId = request.MachineId,
            ProductId = request.ProductId,
            SlotNumber = slot.SlotNumber,
            Amount = slot.Product.Price,
            Currency = slot.Product.Currency,
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow
        };

        transaction.MoveTo(VendingTransactionStatus.AWAITING_PAYMENT);

        await repository.AddTransactionAsync(transaction, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await events.PublishAsync("payment.requested", transaction.CorrelationId, transaction.MachineId, new
        {
            transaction.TransactionId,
            transaction.MachineId,
            transaction.ProductId,
            transaction.SlotNumber,
            transaction.Amount,
            transaction.Currency
        }, cancellationToken);

        return ResponseModel.Ok(transaction.ToDto(), "Product reserved");
    }
}
