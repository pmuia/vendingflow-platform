using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace VendingFlow.MachineSimulator;

public sealed class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    private readonly Dictionary<string, bool> _heartbeatsEnabled = new() { ["VM-001"] = true, ["VM-002"] = true, ["VM-003"] = true, ["VM-004"] = true };
    private bool _vm003Failure = true;
    private IChannel? _channel;
    private string _exchange = "vendingflow.events";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _exchange = configuration["RabbitMq:Exchange"] ?? _exchange;
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:HostName"] ?? "localhost",
            Port = int.TryParse(configuration["RabbitMq:Port"], out var port) ? port : 5672,
            UserName = configuration["RabbitMq:UserName"] ?? "vendingflow",
            Password = configuration["RabbitMq:Password"] ?? "vendingflow"
        };

        var connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync("machine-simulator.dispense", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync("machine-simulator.dispense", _exchange, "product.dispense.requested", cancellationToken: stoppingToken);

        _ = Task.Run(() => ReadCommandsAsync(stoppingToken), stoppingToken);
        _ = Task.Run(() => ConsumeDispenseRequestsAsync(stoppingToken), stoppingToken);

        logger.LogInformation("Simulator commands: fail-vm003 on|off, stop-vm004, resume-vm004");
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var machineId in _heartbeatsEnabled.Where(kvp => kvp.Value).Select(kvp => kvp.Key))
                await PublishAsync("machine.heartbeat", Guid.NewGuid().ToString("N"), machineId, Heartbeat(machineId), stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ConsumeDispenseRequestsAsync(CancellationToken ct)
    {
        if (_channel is null) return;
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var envelope = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
            var correlationId = envelope.GetProperty("correlationId").GetString()!;
            var machineId = envelope.GetProperty("machineId").GetString()!;
            var payload = envelope.GetProperty("payload");
            var transactionId = payload.GetProperty("transactionId").GetString()!;
            await Task.Delay(1200, ct);
            var failed = machineId == "VM-003" && _vm003Failure;
            var eventType = failed ? "product.dispense.failed" : "product.dispensed";
            logger.LogInformation("{EventType} TransactionId={TransactionId} CorrelationId={CorrelationId} MachineId={MachineId}", eventType, transactionId, correlationId, machineId);
            await PublishAsync(eventType, correlationId, machineId, new { transactionId, machineId, reason = failed ? "SIMULATED_DISPENSER_FAILURE" : null }, ct);
            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        };
        await _channel.BasicConsumeAsync("machine-simulator.dispense", false, consumer, ct);
    }

    private async Task ReadCommandsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await Console.In.ReadLineAsync(ct);
            switch (line?.Trim().ToLowerInvariant())
            {
                case "fail-vm003 off":
                    _vm003Failure = false;
                    logger.LogInformation("VM-003 dispenser failure disabled.");
                    break;
                case "fail-vm003 on":
                    _vm003Failure = true;
                    logger.LogInformation("VM-003 dispenser failure enabled.");
                    break;
                case "stop-vm004":
                    _heartbeatsEnabled["VM-004"] = false;
                    logger.LogInformation("VM-004 heartbeats stopped.");
                    break;
                case "resume-vm004":
                    _heartbeatsEnabled["VM-004"] = true;
                    logger.LogInformation("VM-004 heartbeats resumed.");
                    break;
            }
        }
    }

    private object Heartbeat(string machineId) => new
    {
        machineId,
        timestamp = DateTimeOffset.UtcNow,
        temperature = machineId == "VM-003" ? 8.4 : 4.3,
        networkStrength = machineId == "VM-004" ? 67 : 88,
        cashModuleStatus = "OK",
        dispenserStatus = machineId == "VM-003" && _vm003Failure ? "ERROR" : "OK",
        doorStatus = "CLOSED",
        softwareVersion = "1.0.0"
    };

    private async Task PublishAsync(string eventType, string correlationId, string machineId, object payload, CancellationToken ct)
    {
        if (_channel is null) return;
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid().ToString("N"),
            eventType,
            timestamp = DateTimeOffset.UtcNow,
            correlationId,
            machineId,
            payload
        }, JsonOptions));
        await _channel.BasicPublishAsync(_exchange, eventType, true, new BasicProperties { Persistent = true, ContentType = "application/json" }, body, ct);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
