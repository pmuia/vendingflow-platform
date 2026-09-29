namespace PaymentService.Domain.Common;

public abstract class AuditableEntity
{
    public long PartnerId { get; set; }
    public long CompanyId { get; set; }
    public long BranchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public required string CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public byte RecordStatus { get; set; }
}
