using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Queries;

[CommandName("getMachineInventory")]
public record GetMachineInventoryQuery(string MachineId) : IRequest<ResponseModel>;

public class GetMachineInventoryQueryHandler(IInventoryRepository repository) : IRequestHandler<GetMachineInventoryQuery, ResponseModel>
{
    public async Task<ResponseModel> Handle(GetMachineInventoryQuery request, CancellationToken cancellationToken)
    {
        var inventory = await repository.GetInventoryAsync(request.MachineId, cancellationToken);
        return ResponseModel.Ok(inventory.Select(item => item.ToDto()).ToList());
    }
}
