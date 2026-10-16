namespace InventoryService.Application.Interfaces;

public record MachineAvailability(bool CanVend, string Reason);

public interface IMachineAvailabilityService
{
    Task<MachineAvailability> CheckAsync(string machineId, CancellationToken cancellationToken);
}
