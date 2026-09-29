using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Database;

namespace PaymentService.Infrastructure;

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

        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(dbConn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PaymentDbContext).GetTypeInfo().Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__PaymentDbContextMigrationsHistory", "paymentservice");
            }));

        services.Configure<RabbitOptions>(configuration.GetSection("RabbitMq"));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IConnection>(_ => new Connection(dbConn));
        services.AddSingleton<IEventBus, RabbitEventBus>();
        services.AddHostedService<PaymentEventConsumer>();
        return services;
    }
}
