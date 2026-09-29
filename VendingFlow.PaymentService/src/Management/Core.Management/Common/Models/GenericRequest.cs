using System.Text.Json;

namespace PaymentService.Application.Common.Models;

public class GenericRequest
{
    public string Service { get; set; } = string.Empty;
    public JsonDocument? Data { get; set; }
}
