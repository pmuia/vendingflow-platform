namespace InventoryService.Domain.Entities.InventoryModule;

public class Product
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "KES";
    public bool IsActive { get; set; } = true;
}
