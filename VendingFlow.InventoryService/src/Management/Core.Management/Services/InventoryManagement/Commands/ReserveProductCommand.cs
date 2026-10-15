using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using InventoryService.Domain.Entities.InventoryModule;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Commands;

[CommandName("reserveProduct")]
public record ReserveProductCommand(string MachineId, long ProductId, int Quantity, string PaymentMethod,
    string CustomerPhone) : IRequest<ResponseModel>;

public class ReserveProductCommandHandler(IInventoryRepository repository, IEventBus events) : IRequestHandler<ReserveProductCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(ReserveProductCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetSlotAsync(request.MachineId, request.ProductId, cancellationToken) ?? throw new InvalidOperationException("Product unavailable for this machine.");
        if (request.Quantity <= 0) throw new InvalidOperationException("Quantity must be at least one.");
        if (request.Quantity > slot.Quantity) throw new InvalidOperationException($"Only {slot.Quantity} items are available.");
        if (string.IsNullOrWhiteSpace(request.PaymentMethod)) throw new InvalidOperationException("Payment method is required.");
        if (request.PaymentMethod.Equals("MPESA", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.CustomerPhone))
            throw new InvalidOperationException("Phone number is required for M-Pesa STK Push.");

        var transaction = new VendingTransaction
        {
            Id = LongIdGenerator.NextId(),
            TransactionId = $"VTX-{Guid.NewGuid():N}",
            MachineId = request.MachineId,
            ProductId = request.ProductId,
            SlotNumber = slot.SlotNumber,
            Quantity = request.Quantity,
            Amount = slot.Product.Price * request.Quantity,
            Currency = slot.Product.Currency,
            PaymentMethod = request.PaymentMethod.ToUpperInvariant(),
            CustomerPhone = request.CustomerPhone.Trim(),
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
            transaction.Quantity,
            transaction.Amount,
            transaction.Currency,
            transaction.PaymentMethod,
            transaction.CustomerPhone
        }, cancellationToken);

        return ResponseModel.Ok(transaction.ToDto(), "Product reserved");
    }
}
