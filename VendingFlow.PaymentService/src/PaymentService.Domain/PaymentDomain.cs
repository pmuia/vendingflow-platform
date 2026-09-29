namespace PaymentService.Domain;

public enum TransactionType { PAYMENT, REFUND, REVERSAL }
public enum TransactionCode { VEND_PAYMENT, VEND_REFUND, VEND_REVERSAL }
public enum PaymentStatus { PENDING, PROCESSING, CONFIRMED, FAILED, REFUNDED, REVERSED }

public sealed class PaymentTransaction
{
    public Guid Id { get; set; }
    public string PaymentTransactionId { get; set; } = "";
    public string VendingTransactionId { get; set; } = "";
    public string MachineId { get; set; } = "";
    public TransactionType TransactionType { get; set; }
    public TransactionCode TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "KES";
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    public string ProviderReference { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
