namespace PaymentService.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CommandNameAttribute(string commandName) : Attribute
{
    public string CommandName { get; } = commandName;
}
