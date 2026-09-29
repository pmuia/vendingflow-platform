using MachineService.Domain.Entities.MachineModule;

namespace MachineService.Application.Services.MachineManagement.Models;

public record MachineDto(long Id, string MachineCode, string Name, string Location, string Status, DateTimeOffset? LastHeartbeatAt, bool IsActive);

public static class MachineMapping
{
    public static MachineDto ToDto(this Machine machine) =>
        new(machine.Id, machine.MachineCode, machine.Name, machine.Location, machine.Status.ToString(), machine.LastHeartbeatAt, machine.IsActive);
}
