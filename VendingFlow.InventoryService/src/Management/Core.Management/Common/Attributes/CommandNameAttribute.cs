namespace InventoryService.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandNameAttribute(string commandName) : Attribute
{
    public string CommandName { get; } = commandName;
}
