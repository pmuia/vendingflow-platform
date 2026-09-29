using System.Reflection;
using System.Text.Json;
using InventoryService.Application.Common.Attributes;
using InventoryService.Application.Common.Interfaces;
using InventoryService.Application.Common.Models;
using MediatR;

namespace InventoryService.Application.Common.Services;

public sealed class CommandFactory : ICommandFactory
{
    private readonly IReadOnlyDictionary<string, Type> _commands;

    public CommandFactory()
    {
        _commands = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => typeof(IRequest<ResponseModel>).IsAssignableFrom(type))
            .Select(type => new { Type = type, Attribute = type.GetCustomAttribute<CommandNameAttribute>() })
            .Where(item => item.Attribute is not null)
            .ToDictionary(item => item.Attribute!.CommandName, item => item.Type, StringComparer.OrdinalIgnoreCase);
    }

    public IRequest<ResponseModel> Create(GenericRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Service))
            throw new InvalidOperationException("Service is required.");

        if (!_commands.TryGetValue(request.Service, out var commandType))
            throw new InvalidOperationException($"Unknown inventory service command '{request.Service}'.");

        if (request.Data is null)
            return (IRequest<ResponseModel>)Activator.CreateInstance(commandType)!;

        return (IRequest<ResponseModel>)request.Data.RootElement.Deserialize(commandType, JsonDefaults.Options)!;
    }
}
