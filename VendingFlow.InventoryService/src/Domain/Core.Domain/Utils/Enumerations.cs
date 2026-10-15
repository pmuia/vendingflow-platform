namespace InventoryService.Domain.Utils;

public enum VendingTransactionStatus
{
    CREATED,
    AWAITING_PAYMENT,
    PAYMENT_CONFIRMED,
    DISPENSING,
    COMPLETED,
    DISPENSE_FAILED,
    REFUND_REQUIRED,
    REFUNDED,
    FAILED
}

public enum InventoryMovementType
{
    STOCK_IN,
    SALE,
    ADJUSTMENT
}
