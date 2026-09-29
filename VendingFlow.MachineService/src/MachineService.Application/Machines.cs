using MachineService.Domain;
using MediatR;

namespace MachineService.Application;

public record MachineDto(Guid Id, string MachineCode, string Name, string Location, string Status, DateTimeOffset? LastHeartbeatAt, bool IsActive);

public interface IMachineRepository
{
    Task<Machine?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Machine?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Machine>> ListAsync(CancellationToken ct);
    Task AddAsync(Machine machine, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public record RegisterMachineCommand(string MachineCode, string Name, string Location) : IRequest<MachineDto>;
public record UpdateMachineCommand(Guid Id, string Name, string Location, bool IsActive) : IRequest<MachineDto>;
public record UpdateMachineStatusCommand(string MachineCode, MachineStatus Status, DateTimeOffset? LastHeartbeatAt) : IRequest<MachineDto>;
public record GetMachineQuery(string MachineCode) : IRequest<MachineDto?>;
public record GetMachinesQuery : IRequest<IReadOnlyList<MachineDto>>;

public static class MachineMapping
{
    public static MachineDto ToDto(this Machine machine) =>
        new(machine.Id, machine.MachineCode, machine.Name, machine.Location, machine.Status.ToString(), machine.LastHeartbeatAt, machine.IsActive);
}

public sealed class RegisterMachineHandler(IMachineRepository repository) : IRequestHandler<RegisterMachineCommand, MachineDto>
{
    public async Task<MachineDto> Handle(RegisterMachineCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByCodeAsync(request.MachineCode, cancellationToken);
        if (existing is not null) return existing.ToDto();

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
        return machine.ToDto();
    }
}

public sealed class UpdateMachineHandler(IMachineRepository repository) : IRequestHandler<UpdateMachineCommand, MachineDto>
{
    public async Task<MachineDto> Handle(UpdateMachineCommand request, CancellationToken cancellationToken)
    {
        var machine = await repository.GetByIdAsync(request.Id, cancellationToken) ?? throw new InvalidOperationException("Machine not found.");
        machine.Name = request.Name;
        machine.Location = request.Location;
        machine.IsActive = request.IsActive;
        machine.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return machine.ToDto();
    }
}

public sealed class UpdateMachineStatusHandler(IMachineRepository repository) : IRequestHandler<UpdateMachineStatusCommand, MachineDto>
{
    public async Task<MachineDto> Handle(UpdateMachineStatusCommand request, CancellationToken cancellationToken)
    {
        var machine = await repository.GetByCodeAsync(request.MachineCode, cancellationToken) ?? throw new InvalidOperationException("Machine not found.");
        machine.Status = request.Status;
        machine.LastHeartbeatAt = request.LastHeartbeatAt ?? DateTimeOffset.UtcNow;
        machine.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return machine.ToDto();
    }
}

public sealed class GetMachineHandler(IMachineRepository repository) : IRequestHandler<GetMachineQuery, MachineDto?>
{
    public async Task<MachineDto?> Handle(GetMachineQuery request, CancellationToken cancellationToken) =>
        (await repository.GetByCodeAsync(request.MachineCode, cancellationToken))?.ToDto();
}

public sealed class GetMachinesHandler(IMachineRepository repository) : IRequestHandler<GetMachinesQuery, IReadOnlyList<MachineDto>>
{
    public async Task<IReadOnlyList<MachineDto>> Handle(GetMachinesQuery request, CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(m => m.ToDto()).ToList();
}
