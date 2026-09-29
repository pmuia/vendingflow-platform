namespace InventoryService.Application.Common.Models;

public sealed record ResponseModel
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public object? Data { get; init; }

    public static ResponseModel Ok(object? data = null, string message = "Success") => new() { Success = true, Message = message, Data = data };
    public static ResponseModel Fail(string message) => new() { Success = false, Message = message };
}
