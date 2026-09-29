using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Queries;

[CommandName("getProducts")]
public sealed record GetProductsQuery : IRequest<ResponseModel>;

public sealed class GetProductsQueryHandler(IInventoryRepository repository) : IRequestHandler<GetProductsQuery, ResponseModel>
{
    public async Task<ResponseModel> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = (await repository.GetProductsAsync(cancellationToken)).Select(p => p.ToDto()).ToList();
        return ResponseModel.Ok(products);
    }
}
