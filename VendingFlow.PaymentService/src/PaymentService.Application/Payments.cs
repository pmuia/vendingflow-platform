using PaymentService.Domain;
using MediatR;

namespace PaymentService.Application;

public record PaymentDto(string PaymentTransactionId, string VendingTransactionId, string MachineId, string TransactionType, string TransactionCode, decimal Amount, string Currency, string Status, string CorrelationId);

public interface IPaymentRepository
{
    Task<PaymentTransaction?> FindAsync(string vendingTransactionId, TransactionType type, CancellationToken ct);
    Task<IReadOnlyList<PaymentTransaction>> RecentAsync(CancellationToken ct);
    Task AddAsync(PaymentTransaction transaction, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IEventBus
{
    Task PublishAsync(string eventType, string correlationId, string machineId, object payload, CancellationToken ct);
}

public record GetRecentPaymentsQuery : IRequest<IReadOnlyList<PaymentDto>>;

public sealed class GetRecentPaymentsHandler(IPaymentRepository repository) : IRequestHandler<GetRecentPaymentsQuery, IReadOnlyList<PaymentDto>>
{
    public async Task<IReadOnlyList<PaymentDto>> Handle(GetRecentPaymentsQuery request, CancellationToken cancellationToken) =>
        (await repository.RecentAsync(cancellationToken)).Select(ToDto).ToList();

    public static PaymentDto ToDto(PaymentTransaction tx) => new(tx.PaymentTransactionId, tx.VendingTransactionId, tx.MachineId, tx.TransactionType.ToString(), tx.TransactionCode.ToString(), tx.Amount, tx.Currency, tx.Status.ToString(), tx.CorrelationId);
}
