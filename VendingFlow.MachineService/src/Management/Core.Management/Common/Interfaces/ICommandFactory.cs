using MachineService.Application.Common.Models;
using MediatR;

namespace MachineService.Application.Common.Interfaces;

public interface ICommandFactory
{
    IRequest<ResponseModel> Create(GenericRequest request);
}
