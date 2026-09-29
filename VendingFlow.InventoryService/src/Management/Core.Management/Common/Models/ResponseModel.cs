namespace InventoryService.Application.Common.Models;

public class ResponseModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static ResponseModel Ok(object? data = null, string message = "Success") => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static ResponseModel Fail(string message, object? data = null) => new()
    {
        Success = false,
        Message = message,
        Data = data
    };
}
