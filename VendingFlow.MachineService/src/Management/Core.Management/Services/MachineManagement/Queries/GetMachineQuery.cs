using MachineService.Application.Common.Attributes;
using MachineService.Application.Common.Models;
using MachineService.Application.Interfaces;
using MachineService.Application.Services.MachineManagement.Models;
using MediatR;

namespace MachineService.Application.Services.MachineManagement.Queries;

[CommandName("getMachine")]
public record GetMachineQuery(string MachineCode) : IRequest<ResponseModel>;

public class GetMachineQueryHandler(IMachineRepository repository) : IRequestHandler<GetMachineQuery, ResponseModel>
{
    public async Task<ResponseModel> Handle(GetMachineQuery request, CancellationToken cancellationToken)
    {
        var machine = await repository.GetByCodeAsync(request.MachineCode, cancellationToken);
        return ResponseModel.Ok(machine?.ToDto());
    }
}
