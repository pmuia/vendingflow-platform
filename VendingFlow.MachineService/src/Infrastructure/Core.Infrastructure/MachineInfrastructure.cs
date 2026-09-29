using MachineService.Application.Interfaces;
using MachineService.Infrastructure.Database;
using MachineService.Domain.Entities.MachineModule;
using Microsoft.Extensions.DependencyInjection;

namespace MachineService.Infrastructure;

public class MachineRepository(MachineDbContext db) : IMachineRepository
{
    public Task<Machine?> GetByIdAsync(long id, CancellationToken ct) => db.Machines.FirstOrDefaultAsync(m => m.Id == id, ct);
    public Task<Machine?> GetByCodeAsync(string code, CancellationToken ct) => db.Machines.FirstOrDefaultAsync(m => m.MachineCode == code, ct);
    public async Task<IReadOnlyList<Machine>> ListAsync(CancellationToken ct) => await db.Machines.OrderBy(m => m.MachineCode).ToListAsync(ct);
    public async Task AddAsync(Machine machine, CancellationToken ct) => await db.Machines.AddAsync(machine, ct);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public static class MachineSeed
{
    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MachineDbContext>();
        await db.Database.MigrateAsync();
        if (await db.Machines.AnyAsync()) return;

        var now = DateTimeOffset.UtcNow;
        db.Machines.AddRange(
            Seed("VM-001", "Atrium Juice Machine", "Westlands Office Atrium, Nairobi", MachineStatus.ONLINE, now),
            Seed("VM-002", "Cafeteria Juice Machine", "Kilimani Co-working Cafeteria, Nairobi", MachineStatus.ONLINE, now),
            Seed("VM-003", "Lobby Juice Machine", "Upper Hill Medical Plaza Lobby, Nairobi", MachineStatus.DEGRADED, now),
            Seed("VM-004", "Transit Juice Machine", "Nairobi Central Station Concourse", MachineStatus.ONLINE, now));
        await db.SaveChangesAsync();
    }

    private static Machine Seed(string code, string name, string location, MachineStatus status, DateTimeOffset now) => new()
    {
        Id = LongIdGenerator.NextId(),
        MachineCode = code,
        Name = name,
        Location = location,
        Status = status,
        LastHeartbeatAt = now,
        InstalledAt = now.AddMonths(-3),
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };
}
