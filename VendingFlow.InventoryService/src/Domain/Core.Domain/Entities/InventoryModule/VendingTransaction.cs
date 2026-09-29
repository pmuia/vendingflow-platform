namespace InventoryService.Domain.Entities.InventoryModule;

public sealed class VendingTransaction
{
    public Guid Id { get; set; }
    public string TransactionId { get; set; } = "";
    public string MachineId { get; set; } = "";
    public Guid ProductId { get; set; }
    public int SlotNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "KES";
    public VendingTransactionStatus Status { get; private set; } = VendingTransactionStatus.CREATED;
    public string CorrelationId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public void MoveTo(VendingTransactionStatus next)
    {
        var valid = Status switch
        {
            VendingTransactionStatus.CREATED => next == VendingTransactionStatus.AWAITING_PAYMENT,
            VendingTransactionStatus.AWAITING_PAYMENT => next is VendingTransactionStatus.PAYMENT_CONFIRMED or VendingTransactionStatus.FAILED,
            VendingTransactionStatus.PAYMENT_CONFIRMED => next == VendingTransactionStatus.DISPENSING,
            VendingTransactionStatus.DISPENSING => next is VendingTransactionStatus.COMPLETED or VendingTransactionStatus.DISPENSE_FAILED,
            VendingTransactionStatus.DISPENSE_FAILED => next == VendingTransactionStatus.REFUND_REQUIRED,
            VendingTransactionStatus.REFUND_REQUIRED => next == VendingTransactionStatus.REFUNDED,
            _ => false
        };

        if (!valid) throw new InvalidOperationException($"Invalid vending transition {Status} -> {next}.");

        Status = next;
        if (next is VendingTransactionStatus.COMPLETED or VendingTransactionStatus.REFUNDED or VendingTransactionStatus.FAILED)
            CompletedAt = DateTimeOffset.UtcNow;
    }
}
