using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Queries;

[CommandName("getItemLedger")]
public record GetItemLedgerQuery(string? MachineId = null) : IRequest<ResponseModel>;

public class GetItemLedgerQueryHandler(IInventoryRepository repository) : IRequestHandler<GetItemLedgerQuery, ResponseModel>
{
    public async Task<ResponseModel> Handle(GetItemLedgerQuery request, CancellationToken cancellationToken)
    {
        var entries = await repository.GetItemLedgerAsync(request.MachineId, cancellationToken);
        return ResponseModel.Ok(entries.Select(entry => entry.ToDto()).ToList());
    }
}
