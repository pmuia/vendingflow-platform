using System.Text.Json;

namespace PaymentService.Application.Common.Models;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
