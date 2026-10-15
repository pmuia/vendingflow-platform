using PaymentService.Domain.Entities.PaymentModule;

namespace PaymentService.Application.Services.PaymentManagement.Models;

public record PaymentDto(long PaymentTransactionId, string PaymentTransactionCode, string VendingTransactionId,
    string MachineId, string TransactionType, string TransactionCode, decimal Amount, string Currency,
    string PaymentMethod, string CustomerPhone, string Status, string ProviderReference, string CorrelationId,
    DateTime CreatedAt, DateTimeOffset? CompletedAt);

public static class PaymentMapping
{
    public static PaymentDto ToDto(this PaymentTransaction transaction) => new(
        transaction.PaymentTransactionId,
        transaction.PaymentTransactionCode,
        transaction.VendingTransactionId,
        transaction.MachineId,
        transaction.TransactionType.ToString(),
        transaction.TransactionCode.ToString(),
        transaction.Amount,
        transaction.Currency,
        transaction.PaymentMethod,
        transaction.CustomerPhone,
        transaction.Status.ToString(),
        transaction.ProviderReference,
        transaction.CorrelationId,
        transaction.CreatedAt,
        transaction.CompletedAt);
}
