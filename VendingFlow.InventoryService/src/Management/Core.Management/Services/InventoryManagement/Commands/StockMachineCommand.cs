using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Commands;

[CommandName("stockMachine")]
public record StockMachineCommand(string MachineId, Guid ProductId, int Quantity) : IRequest<ResponseModel>;

public class StockMachineCommandHandler(IInventoryRepository repository, IEventBus events) : IRequestHandler<StockMachineCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(StockMachineCommand request, CancellationToken cancellationToken)
    {
        var slot = await repository.GetSlotAsync(request.MachineId, request.ProductId, cancellationToken) ?? throw new InvalidOperationException("Slot not found.");
        slot.Quantity = Math.Min(slot.Capacity, slot.Quantity + request.Quantity);
        slot.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
        await events.PublishAsync("inventory.updated", Guid.NewGuid().ToString("N"), request.MachineId, slot.ToDto(), cancellationToken);

        return ResponseModel.Ok(slot.ToDto(), "Machine stocked");
    }
}
