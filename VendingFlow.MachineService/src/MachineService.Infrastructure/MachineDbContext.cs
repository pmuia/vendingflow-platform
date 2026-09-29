using MachineService.Application;
using MachineService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MachineService.Infrastructure;

public sealed class MachineDbContext(DbContextOptions<MachineDbContext> options) : DbContext(options)
{
    public DbSet<Machine> Machines => Set<Machine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Machine>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.MachineCode).IsUnique();
            entity.Property(m => m.MachineCode).HasMaxLength(32).IsRequired();
            entity.Property(m => m.Name).HasMaxLength(120).IsRequired();
            entity.Property(m => m.Location).HasMaxLength(200).IsRequired();
            entity.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);
        });
    }
}

public sealed class MachineRepository(MachineDbContext db) : IMachineRepository
{
    public Task<Machine?> GetByIdAsync(Guid id, CancellationToken ct) => db.Machines.FirstOrDefaultAsync(m => m.Id == id, ct);
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
        await db.Database.EnsureCreatedAsync();
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
        Id = Guid.NewGuid(),
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
