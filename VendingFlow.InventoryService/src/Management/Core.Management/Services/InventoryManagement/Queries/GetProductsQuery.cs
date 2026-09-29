using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Queries;

[CommandName("getProducts")]
public record GetProductsQuery : IRequest<ResponseModel>;

public class GetProductsQueryHandler(IInventoryRepository repository) : IRequestHandler<GetProductsQuery, ResponseModel>
{
    public async Task<ResponseModel> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await repository.GetProductsAsync(cancellationToken);
        return ResponseModel.Ok(products.Select(product => product.ToDto()).ToList());
    }
}
