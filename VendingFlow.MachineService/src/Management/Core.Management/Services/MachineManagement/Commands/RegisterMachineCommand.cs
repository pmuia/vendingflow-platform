using MachineService.Application.Common.Attributes;
using MachineService.Application.Common.Models;
using MachineService.Application.Interfaces;
using MachineService.Application.Services.MachineManagement.Models;
using MachineService.Domain.Entities.MachineModule;
using MediatR;

namespace MachineService.Application.Services.MachineManagement.Commands;

[CommandName("registerMachine")]
public record RegisterMachineCommand(string MachineCode, string Name, string Location) : IRequest<ResponseModel>;

public class RegisterMachineCommandHandler(IMachineRepository repository) : IRequestHandler<RegisterMachineCommand, ResponseModel>
{
    public async Task<ResponseModel> Handle(RegisterMachineCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByCodeAsync(request.MachineCode, cancellationToken);
        if (existing is not null) return ResponseModel.Ok(existing.ToDto(), "Machine already exists");

        var now = DateTimeOffset.UtcNow;
        var machine = new Machine
        {
            Id = Guid.NewGuid(),
            MachineCode = request.MachineCode,
            Name = request.Name,
            Location = request.Location,
            Status = MachineStatus.OFFLINE,
            InstalledAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(machine, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ResponseModel.Ok(machine.ToDto(), "Machine registered");
    }
}
