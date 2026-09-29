using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Commands;

[CommandName("stockMachine")]
public sealed record StockMachineCommand(string MachineId, Guid ProductId, int Quantity) : IRequest<ResponseModel>;

public sealed class StockMachineCommandHandler(IInventoryRepository repository, IEventBus events) : IRequestHandler<StockMachineCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(StockMachineCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0) throw new InvalidOperationException("Stock quantity must be greater than zero.");

        var slot = await repository.GetSlotAsync(request.MachineId, request.ProductId, cancellationToken) ?? throw new InvalidOperationException("Slot not found.");
        slot.Quantity = Math.Min(slot.Capacity, slot.Quantity + request.Quantity);
        slot.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        var dto = slot.ToDto();
        await events.PublishAsync("inventory.updated", Guid.NewGuid().ToString("N"), request.MachineId, dto, cancellationToken);
        return ResponseModel.Ok(dto, "Machine stocked");
    }
}
