using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.Infrastructure;

public class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    public Task<PaymentTransaction?> FindAsync(string vendingTransactionId, TransactionType type, CancellationToken ct) =>
        db.PaymentTransactions.FirstOrDefaultAsync(payment => payment.VendingTransactionId == vendingTransactionId && payment.TransactionType == type, ct);

    public async Task<IReadOnlyList<PaymentTransaction>> RecentAsync(CancellationToken ct) => await db.PaymentTransactions
        .OrderByDescending(payment => payment.CreatedAt)
        .Take(25)
        .ToListAsync(ct);

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct) => await db.PaymentTransactions.AddAsync(transaction, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public class RabbitOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "vendingflow";
    public string Password { get; set; } = "vendingflow";
    public string Exchange { get; set; } = "vendingflow.events";
}

public class RabbitEventBus(IOptions<RabbitOptions> options) : IEventBus
{
    public async Task PublishAsync(string eventType, string correlationId, string machineId, object payload, CancellationToken ct)
    {
        var config = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password
        };

        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.ExchangeDeclareAsync(config.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid().ToString("N"),
            eventType,
            timestamp = DateTimeOffset.UtcNow,
            correlationId,
            machineId,
            payload
        }, JsonDefaults.Options));

        await channel.BasicPublishAsync(config.Exchange, eventType, true, new BasicProperties { Persistent = true, ContentType = "application/json" }, body, ct);
    }
}

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

public class PaymentEventConsumer(IServiceProvider services, IOptions<RabbitOptions> options, ILogger<PaymentEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password
        };

        var connection = await factory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(config.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync("payment.processor", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync("payment.processor", config.Exchange, "payment.requested", cancellationToken: stoppingToken);
        await channel.QueueBindAsync("payment.processor", config.Exchange, "payment.refund.requested", cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                await HandleAsync(Encoding.UTF8.GetString(eventArgs.Body.ToArray()), stoppingToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment event processing failed.");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false, stoppingToken);
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

        var transaction = new PaymentTransaction
        {
            PaymentTransactionId = LongIdGenerator.NextId(),
            PaymentTransactionCode = $"PTX-{Guid.NewGuid():N}",
            VendingTransactionId = vendingTransactionId,
            MachineId = machineId,
            TransactionType = type,
            TransactionCode = type == TransactionType.REFUND ? TransactionCode.VEND_REFUND : TransactionCode.VEND_PAYMENT,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.PROCESSING,
            ProviderReference = $"SIM-{Guid.NewGuid():N}",
            CorrelationId = correlationId,
            PartnerId = 0,
            CompanyId = 0,
            BranchId = 0,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            CreatedBy = "payment-service",
            RecordStatus = 1,
            ProcessedAt = DateTimeOffset.UtcNow
        };

        transaction.Status = type == TransactionType.REFUND ? PaymentStatus.REFUNDED : PaymentStatus.CONFIRMED;
        transaction.CompletedAt = DateTimeOffset.UtcNow;

        await repository.AddAsync(transaction, ct);
        await repository.SaveChangesAsync(ct);

        var published = type == TransactionType.REFUND ? "payment.refunded" : "payment.confirmed";
        logger.LogInformation("{EventType} TransactionId={TransactionId} CorrelationId={CorrelationId} MachineId={MachineId}", published, vendingTransactionId, correlationId, machineId);
        await bus.PublishAsync(published, correlationId, machineId, new { transactionId = vendingTransactionId, paymentTransactionId = transaction.PaymentTransactionCode, machineId, amount, currency }, ct);
    }
}

public static class PaymentSeed
{
    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await db.Database.MigrateAsync();
    }
}
