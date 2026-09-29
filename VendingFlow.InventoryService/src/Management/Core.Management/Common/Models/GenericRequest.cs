using System.Text.Json;

namespace InventoryService.Application.Common.Models;

public class GenericRequest
{
    public string Service { get; set; } = string.Empty;
    public JsonDocument? Data { get; set; }
}
