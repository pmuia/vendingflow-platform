namespace InventoryService.Domain;

public sealed class Product
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "KES";
    public bool IsActive { get; set; } = true;
}

public sealed class MachineInventory
{
    public Guid Id { get; set; }
    public string MachineId { get; set; } = "";
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int SlotNumber { get; set; }
    public int Quantity { get; set; }
    public int Capacity { get; set; }
    public int LowStockThreshold { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

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
