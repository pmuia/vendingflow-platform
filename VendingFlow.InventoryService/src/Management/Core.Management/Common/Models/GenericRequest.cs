using System.Text.Json;

namespace InventoryService.Application.Common.Models;

public sealed record GenericRequest
{
    public string Service { get; init; } = "";
    public JsonDocument? Data { get; init; }

    public GenericRequest() { }

    public GenericRequest(string service, object data)
    {
        Service = service;
        Data = JsonSerializer.SerializeToDocument(data, JsonDefaults.Options);
    }
}
