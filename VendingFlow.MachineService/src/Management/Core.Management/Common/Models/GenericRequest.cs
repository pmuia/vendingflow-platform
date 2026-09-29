using System.Text.Json;

namespace MachineService.Application.Common.Models;

public record GenericRequest
{
    public string Service { get; init; } = "";
    public JsonDocument? Data { get; init; }
}
