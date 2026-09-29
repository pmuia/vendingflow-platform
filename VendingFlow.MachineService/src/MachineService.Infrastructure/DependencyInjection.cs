using MachineService.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MachineService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var defaultConnection = configuration["ConnectionStrings:DefaultConnection"];
        var dbConn =
            (!string.IsNullOrWhiteSpace(defaultConnection) ? Environment.GetEnvironmentVariable(defaultConnection) : null) ??
            configuration.GetConnectionString("DefaultConnection") ??
            configuration.GetConnectionString("MachineDb") ??
            "Host=localhost;Port=5432;Database=machine_db;Username=vendingflow;Password=vendingflow";

        services.AddDbContext<MachineDbContext>(options =>
            options.UseNpgsql(dbConn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(MachineDbContext).GetTypeInfo().Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__MachineDbContextMigrationsHistory", "machineservice");
            }));

        services.AddMemoryCache();
        services.AddScoped<IMachineRepository, MachineRepository>();
        return services;
    }
}
