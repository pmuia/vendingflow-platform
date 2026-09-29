namespace MachineService.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class CommandNameAttribute(string commandName) : Attribute
{
    public string CommandName { get; } = commandName;
}
