namespace PaymentService.Application.Interfaces;

public interface IEventBus
{
    Task PublishAsync(string eventType, string correlationId, string machineId, object payload, CancellationToken ct);
}
