using System.Text;
using System.Text.Json;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services.InventoryManagement.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService.Infrastructure;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<MachineInventory> MachineInventories => Set<MachineInventory>();
    public DbSet<VendingTransaction> VendingTransactions => Set<VendingTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventoryservice");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.HasIndex(product => product.ProductCode).IsUnique();
            entity.Property(product => product.Price).HasPrecision(12, 2);
        });

        modelBuilder.Entity<MachineInventory>(entity =>
        {
            entity.HasKey(inventory => inventory.Id);
            entity.HasIndex(inventory => new { inventory.MachineId, inventory.SlotNumber }).IsUnique();
            entity.HasOne(inventory => inventory.Product).WithMany().HasForeignKey(inventory => inventory.ProductId);
        });

        modelBuilder.Entity<VendingTransaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.HasIndex(transaction => transaction.TransactionId).IsUnique();
            entity.Property(transaction => transaction.Amount).HasPrecision(12, 2);
            entity.Property(transaction => transaction.Status).HasConversion<string>().HasMaxLength(32);
        });
    }
}

public class InventoryRepository(InventoryDbContext db) : IInventoryRepository
{
    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken ct) => await db.Products.OrderBy(product => product.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<MachineInventory>> GetInventoryAsync(string machineId, CancellationToken ct) => await db.MachineInventories
        .Include(inventory => inventory.Product)
        .Where(inventory => inventory.MachineId == machineId)
        .OrderBy(inventory => inventory.SlotNumber)
        .ToListAsync(ct);

    public Task<MachineInventory?> GetSlotAsync(string machineId, Guid productId, CancellationToken ct) => db.MachineInventories
        .Include(inventory => inventory.Product)
        .FirstOrDefaultAsync(inventory => inventory.MachineId == machineId && inventory.ProductId == productId, ct);

    public Task<VendingTransaction?> GetTransactionAsync(string transactionId, CancellationToken ct) => db.VendingTransactions.FirstOrDefaultAsync(transaction => transaction.TransactionId == transactionId, ct);

    public async Task<IReadOnlyList<VendingTransaction>> RecentTransactionsAsync(CancellationToken ct) => await db.VendingTransactions
        .OrderByDescending(transaction => transaction.CreatedAt)
        .Take(25)
        .ToListAsync(ct);

    public async Task AddTransactionAsync(VendingTransaction transaction, CancellationToken ct) => await db.VendingTransactions.AddAsync(transaction, ct);

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

        var envelope = new EventEnvelope(Guid.NewGuid().ToString("N"), eventType, DateTimeOffset.UtcNow, correlationId, machineId, payload);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonDefaults.Options));
        await channel.BasicPublishAsync(config.Exchange, eventType, true, new BasicProperties { Persistent = true, ContentType = "application/json" }, body, ct);
    }
}

public record EventEnvelope(string EventId, string EventType, DateTimeOffset Timestamp, string CorrelationId, string MachineId, object Payload);

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

public class InventoryEventConsumer(IServiceProvider services, IOptions<RabbitOptions> options, ILogger<InventoryEventConsumer> logger) : BackgroundService
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
        await channel.QueueDeclareAsync("inventory.vending", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        foreach (var key in new[] { "payment.confirmed", "payment.failed", "payment.refunded", "product.dispensed", "product.dispense.failed" })
            await channel.QueueBindAsync("inventory.vending", config.Exchange, key, cancellationToken: stoppingToken);

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
                logger.LogError(ex, "Inventory event failed: {Message}", ex.Message);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync("inventory.vending", false, consumer, stoppingToken);
    }

    private async Task HandleAsync(string json, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<JsonElement>(json, JsonDefaults.Options);
        var eventType = envelope.GetProperty("eventType").GetString()!;
        var correlationId = envelope.GetProperty("correlationId").GetString()!;
        var machineId = envelope.GetProperty("machineId").GetString()!;
        var payload = envelope.GetProperty("payload");
        var transactionId = payload.GetProperty("transactionId").GetString()!;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var transaction = await db.VendingTransactions.FirstOrDefaultAsync(item => item.TransactionId == transactionId, ct);
        if (transaction is null) return;

        if (eventType == "payment.confirmed" && transaction.Status == VendingTransactionStatus.AWAITING_PAYMENT)
        {
            transaction.MoveTo(VendingTransactionStatus.PAYMENT_CONFIRMED);
            transaction.MoveTo(VendingTransactionStatus.DISPENSING);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Dispense requested TransactionId={TransactionId} CorrelationId={CorrelationId} MachineId={MachineId}", transaction.TransactionId, correlationId, machineId);
            await bus.PublishAsync("product.dispense.requested", correlationId, machineId, new { transaction.TransactionId, transaction.MachineId, transaction.ProductId, transaction.SlotNumber }, ct);
            return;
        }

        if (eventType == "product.dispensed" && transaction.Status == VendingTransactionStatus.DISPENSING)
        {
            var slot = await db.MachineInventories.Include(inventory => inventory.Product).FirstAsync(inventory => inventory.MachineId == transaction.MachineId && inventory.ProductId == transaction.ProductId, ct);
            var before = slot.Quantity;
            if (slot.Quantity <= 0) throw new InvalidOperationException("Inventory would become negative.");

            slot.Quantity -= 1;
            slot.UpdatedAt = DateTimeOffset.UtcNow;
            transaction.MoveTo(VendingTransactionStatus.COMPLETED);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Inventory reduced from {Before} to {After}; transaction completed TransactionId={TransactionId} CorrelationId={CorrelationId}", before, slot.Quantity, transaction.TransactionId, correlationId);
            await bus.PublishAsync("inventory.updated", correlationId, machineId, slot.ToDto(), ct);
            if (slot.Quantity <= slot.LowStockThreshold)
                await bus.PublishAsync("inventory.low", correlationId, machineId, slot.ToDto(), ct);

            return;
        }

        if (eventType == "product.dispense.failed" && transaction.Status == VendingTransactionStatus.DISPENSING)
        {
            transaction.MoveTo(VendingTransactionStatus.DISPENSE_FAILED);
            transaction.MoveTo(VendingTransactionStatus.REFUND_REQUIRED);
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Refund requested after dispenser failure TransactionId={TransactionId} CorrelationId={CorrelationId}", transaction.TransactionId, correlationId);
            await bus.PublishAsync("payment.refund.requested", correlationId, machineId, new { transaction.TransactionId, transaction.MachineId, transaction.Amount, transaction.Currency }, ct);
            return;
        }

        if (eventType == "payment.refunded" && transaction.Status == VendingTransactionStatus.REFUND_REQUIRED)
        {
            transaction.MoveTo(VendingTransactionStatus.REFUNDED);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Refund completed TransactionId={TransactionId} CorrelationId={CorrelationId}", transaction.TransactionId, correlationId);
        }
    }
}

public static class InventorySeed
{
    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await db.Database.MigrateAsync();
        if (await db.Products.AnyAsync()) return;

        var names = new[]
        {
            "Orange Juice", "Mango Juice", "Apple Juice", "Pineapple Juice", "Passion Juice",
            "Guava Juice", "Watermelon Juice", "Tamarind Juice", "Lemonade", "Ginger Lime Juice",
            "Carrot Orange Juice", "Beetroot Berry Juice", "Mixed Berry Juice", "Grape Juice",
            "Peach Juice", "Kiwi Juice", "Cranberry Juice", "Coconut Water", "Strawberry Juice",
            "Pomegranate Juice"
        };

        var products = names.Select((name, index) => new Product
        {
            Id = Guid.NewGuid(),
            ProductCode = $"JCE-{index + 1:000}",
            Name = name,
            Description = $"{name} in a chilled 350ml bottle",
            Price = 120 + (index % 5) * 15,
            Currency = "KES",
            IsActive = true
        }).ToList();

        db.Products.AddRange(products);

        var mango = products.Single(product => product.Name == "Mango Juice");
        var machines = new[] { "VM-001", "VM-002", "VM-003", "VM-004" };
        var slot = 1;

        foreach (var machine in machines)
        {
            foreach (var product in products.Take(8))
            {
                db.MachineInventories.Add(new MachineInventory
                {
                    Id = Guid.NewGuid(),
                    MachineId = machine,
                    ProductId = product.Id,
                    SlotNumber = slot++,
                    Quantity = machine == "VM-002" ? 3 : product.Id == mango.Id ? 20 : 12,
                    Capacity = 25,
                    LowStockThreshold = 3,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            slot = 1;
        }

        await db.SaveChangesAsync();
    }
}
