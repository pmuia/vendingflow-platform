using InventoryService.Application.Common.Models;
using MediatR;

namespace InventoryService.Application.Common.Interfaces;

public interface ICommandFactory
{
    IRequest<ResponseModel> Create(GenericRequest request);
}
