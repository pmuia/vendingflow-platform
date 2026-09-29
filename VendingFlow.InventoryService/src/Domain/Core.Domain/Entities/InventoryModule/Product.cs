namespace InventoryService.Domain.Entities.InventoryModule;

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
