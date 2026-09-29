using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Application;
using PaymentService.Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Infrastructure;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.VendingTransactionId, p.TransactionType }).IsUnique();
            e.Property(p => p.Amount).HasPrecision(12, 2);
            e.Property(p => p.TransactionType).HasConversion<string>().HasMaxLength(32);
            e.Property(p => p.TransactionCode).HasConversion<string>().HasMaxLength(32);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
        });
    }
}

public sealed class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    public Task<PaymentTransaction?> FindAsync(string vendingTransactionId, TransactionType type, CancellationToken ct) => db.PaymentTransactions.FirstOrDefaultAsync(p => p.VendingTransactionId == vendingTransactionId && p.TransactionType == type, ct);
    public async Task<IReadOnlyList<PaymentTransaction>> RecentAsync(CancellationToken ct) => await db.PaymentTransactions.OrderByDescending(p => p.CreatedAt).Take(25).ToListAsync(ct);
    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct) => await db.PaymentTransactions.AddAsync(transaction, ct);
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public sealed class RabbitOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "vendingflow";
    public string Password { get; set; } = "vendingflow";
    public string Exchange { get; set; } = "vendingflow.events";
}

public sealed class RabbitEventBus(IOptions<RabbitOptions> options) : IEventBus
{
    public async Task PublishAsync(string eventType, string correlationId, string machineId, object payload, CancellationToken ct)
    {
        var cfg = options.Value;
        var factory = new ConnectionFactory { HostName = cfg.HostName, Port = cfg.Port, UserName = cfg.UserName, Password = cfg.Password };
        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.ExchangeDeclareAsync(cfg.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid().ToString("N"),
            eventType,
            timestamp = DateTimeOffset.UtcNow,
            correlationId,
            machineId,
            payload
        }, JsonDefaults.Options));
        await channel.BasicPublishAsync(cfg.Exchange, eventType, true, new BasicProperties { Persistent = true, ContentType = "application/json" }, body, ct);
    }
}

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

public sealed class PaymentEventConsumer(IServiceProvider services, IOptions<RabbitOptions> options, ILogger<PaymentEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = options.Value;
        var factory = new ConnectionFactory { HostName = cfg.HostName, Port = cfg.Port, UserName = cfg.UserName, Password = cfg.Password };
        var connection = await factory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(cfg.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync("payment.processor", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync("payment.processor", cfg.Exchange, "payment.requested", cancellationToken: stoppingToken);
        await channel.QueueBindAsync("payment.processor", cfg.Exchange, "payment.refund.requested", cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await HandleAsync(Encoding.UTF8.GetString(ea.Body.ToArray()), stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment event processing failed.");
                await channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }
        };
        await channel.BasicConsumeAsync("payment.processor", false, consumer, stoppingToken);
    }

    private async Task HandleAsync(string json, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<JsonElement>(json, JsonDefaults.Options);
        var eventType = envelope.GetProperty("eventType").GetString()!;
        var correlationId = envelope.GetProperty("correlationId").GetString()!;
        var machineId = envelope.GetProperty("machineId").GetString()!;
        var payload = envelope.GetProperty("payload");
        var vendingTransactionId = payload.GetProperty("transactionId").GetString()!;
        var amount = payload.GetProperty("amount").GetDecimal();
        var currency = payload.GetProperty("currency").GetString()!;
        var type = eventType == "payment.refund.requested" ? TransactionType.REFUND : TransactionType.PAYMENT;

        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var existing = await repository.FindAsync(vendingTransactionId, type, ct);
        if (existing is not null)
        {
            logger.LogInformation("Duplicate payment event ignored VendingTransactionId={TransactionId} Type={Type} CorrelationId={CorrelationId}", vendingTransactionId, type, correlationId);
            return;
        }

        var tx = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = $"PTX-{Guid.NewGuid():N}",
            VendingTransactionId = vendingTransactionId,
            MachineId = machineId,
            TransactionType = type,
            TransactionCode = type == TransactionType.REFUND ? TransactionCode.VEND_REFUND : TransactionCode.VEND_PAYMENT,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.PROCESSING,
            ProviderReference = $"SIM-{Guid.NewGuid():N}",
            CorrelationId = correlationId,
            CreatedAt = DateTimeOffset.UtcNow,
            ProcessedAt = DateTimeOffset.UtcNow
        };
        tx.Status = type == TransactionType.REFUND ? PaymentStatus.REFUNDED : PaymentStatus.CONFIRMED;
        tx.CompletedAt = DateTimeOffset.UtcNow;
        await repository.AddAsync(tx, ct);
        await repository.SaveChangesAsync(ct);

        var published = type == TransactionType.REFUND ? "payment.refunded" : "payment.confirmed";
        logger.LogInformation("{EventType} TransactionId={TransactionId} CorrelationId={CorrelationId} MachineId={MachineId}", published, vendingTransactionId, correlationId, machineId);
        await bus.PublishAsync(published, correlationId, machineId, new { transactionId = vendingTransactionId, paymentTransactionId = tx.PaymentTransactionId, machineId, amount, currency }, ct);
    }
}

public static class PaymentSeed
{
    public static async Task EnsureCreatedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
