using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application;
using System.Reflection;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var defaultConnection = configuration["ConnectionStrings:DefaultConnection"];
        var dbConn =
            (!string.IsNullOrWhiteSpace(defaultConnection) ? Environment.GetEnvironmentVariable(defaultConnection) : null) ??
            configuration.GetConnectionString("DefaultConnection") ??
            configuration.GetConnectionString("PaymentDb") ??
            "Host=localhost;Port=5432;Database=payment_db;Username=vendingflow;Password=vendingflow";

        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(dbConn, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PaymentDbContext).GetTypeInfo().Assembly.GetName().Name);
                npgsql.MigrationsHistoryTable("__PaymentDbContextMigrationsHistory", "paymentservice");
            }));

        services.AddMemoryCache();
        services.Configure<RabbitOptions>(configuration.GetSection("RabbitMq"));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IEventBus, RabbitEventBus>();
        services.AddHostedService<PaymentEventConsumer>();
        return services;
    }
}
