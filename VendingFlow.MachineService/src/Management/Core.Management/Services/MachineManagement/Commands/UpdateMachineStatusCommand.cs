using MachineService.Application.Common.Attributes;
using MachineService.Application.Common.Models;
using MachineService.Application.Interfaces;
using MachineService.Application.Services.MachineManagement.Models;
using MediatR;

namespace MachineService.Application.Services.MachineManagement.Commands;

[CommandName("updateMachineStatus")]
public record UpdateMachineStatusCommand(string MachineCode, MachineStatus Status, DateTimeOffset? LastHeartbeatAt) : IRequest<ResponseModel>;

public class UpdateMachineStatusCommandHandler(IMachineRepository repository) : IRequestHandler<UpdateMachineStatusCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(UpdateMachineStatusCommand request, CancellationToken cancellationToken)
    {
        var machine = await repository.GetByCodeAsync(request.MachineCode, cancellationToken) ?? throw new InvalidOperationException("Machine not found.");
        machine.Status = request.Status;
        machine.LastHeartbeatAt = request.LastHeartbeatAt ?? DateTimeOffset.UtcNow;
        machine.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseModel.Ok(machine.ToDto(), "Machine status updated");
    }
}
