namespace PaymentService.Domain.Entities.PaymentModule;

public class PaymentTransaction : AuditableEntity
{
    public long PaymentTransactionId { get; set; }
    public string PaymentTransactionCode { get; set; } = string.Empty;
    public string VendingTransactionId { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public TransactionCode TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "KES";
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    public string ProviderReference { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
