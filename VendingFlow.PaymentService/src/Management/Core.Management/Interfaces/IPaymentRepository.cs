using PaymentService.Domain.Entities.PaymentModule;

namespace PaymentService.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentTransaction?> FindAsync(string vendingTransactionId, TransactionType type, CancellationToken ct);
    Task<IReadOnlyList<PaymentTransaction>> RecentAsync(CancellationToken ct);
    Task AddAsync(PaymentTransaction transaction, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
