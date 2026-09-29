using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using MediatR;

namespace InventoryService.Application.Services.InventoryManagement.Queries;

[CommandName("getRecentTransactions")]
public record GetRecentTransactionsQuery : IRequest<ResponseModel>;

public class GetRecentTransactionsQueryHandler(IInventoryRepository repository) : IRequestHandler<GetRecentTransactionsQuery, ResponseModel>
{
    public async Task<ResponseModel> Handle(GetRecentTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await repository.RecentTransactionsAsync(cancellationToken);
        return ResponseModel.Ok(transactions.Select(transaction => transaction.ToDto()).ToList());
    }
}
