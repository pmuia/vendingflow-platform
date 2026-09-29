using MachineService.Application.Common.Attributes;
using MachineService.Application.Common.Models;
using MachineService.Application.Interfaces;
using MachineService.Application.Services.MachineManagement.Models;
using MediatR;

namespace MachineService.Application.Services.MachineManagement.Queries;

[CommandName("getMachines")]
public record GetMachinesQuery : IRequest<ResponseModel>;

public class GetMachinesQueryHandler(IMachineRepository repository) : IRequestHandler<GetMachinesQuery, ResponseModel>
{
    public async Task<ResponseModel> Handle(GetMachinesQuery request, CancellationToken cancellationToken)
    {
        var machines = (await repository.ListAsync(cancellationToken)).Select(m => m.ToDto()).ToList();
        return ResponseModel.Ok(machines);
    }
}
