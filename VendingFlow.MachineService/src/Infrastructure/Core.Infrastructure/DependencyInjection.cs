using MachineService.Application.Interfaces;
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
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(dbConn))
            throw new InvalidOperationException("DefaultConnection is missing from both environment variables and appsettings.json.");

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
