namespace PaymentService.Domain.Utils;

public enum TransactionType
{
    PAYMENT,
    REFUND,
    REVERSAL
}

public enum TransactionCode
{
    VEND_PAYMENT,
    VEND_REFUND,
    VEND_REVERSAL
}

public enum PaymentStatus
{
    PENDING,
    PROCESSING,
    CONFIRMED,
    FAILED,
    REFUNDED,
    REVERSED
}
