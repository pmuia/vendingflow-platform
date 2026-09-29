using MachineService.Application.Common.Attributes;
using MachineService.Application.Common.Models;
using MachineService.Application.Interfaces;
using MachineService.Application.Services.MachineManagement.Models;
using MediatR;

namespace MachineService.Application.Services.MachineManagement.Commands;

[CommandName("updateMachine")]
public record UpdateMachineCommand(Guid Id, string Name, string Location, bool IsActive) : IRequest<ResponseModel>;

public class UpdateMachineCommandHandler(IMachineRepository repository) : IRequestHandler<UpdateMachineCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(UpdateMachineCommand request, CancellationToken cancellationToken)
    {
        var machine = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new InvalidOperationException("Machine not found.");
        machine.Name = request.Name;
        machine.Location = request.Location;
        machine.IsActive = request.IsActive;
        machine.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseModel.Ok(machine.ToDto(), "Machine updated");
    }
}
