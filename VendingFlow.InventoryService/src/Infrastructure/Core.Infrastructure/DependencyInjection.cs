using InventoryService.Application;
using InventoryService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace InventoryService.Infrastructure;

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

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(dbConn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(InventoryDbContext).GetTypeInfo().Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__InventoryDbContextMigrationsHistory", "inventoryservice");
            }));

        services.AddMemoryCache();
        services.Configure<RabbitOptions>(configuration.GetSection("RabbitMq"));
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IEventBus, RabbitEventBus>();
        services.AddHostedService<InventoryEventConsumer>();
        return services;
    }
}
