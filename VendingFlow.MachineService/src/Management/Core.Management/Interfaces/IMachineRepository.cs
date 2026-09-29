using MachineService.Domain.Entities.MachineModule;

namespace MachineService.Application.Interfaces;

public interface IMachineRepository
{
    Task<Machine?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Machine?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Machine>> ListAsync(CancellationToken ct);
    Task AddAsync(Machine machine, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
