using System.Reflection;

namespace MachineService.Infrastructure.Database;

public class MachineDbContext(DbContextOptions<MachineDbContext> options) : DbContext(options)
{
    public DbSet<Machine> Machines => Set<Machine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
