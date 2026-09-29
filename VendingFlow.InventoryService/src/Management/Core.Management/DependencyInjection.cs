using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using InventoryService.Application.Common.Interfaces;
using InventoryService.Application.Common.Services;

namespace InventoryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddMediatR(Assembly.GetExecutingAssembly());
        services.AddSingleton<ICommandFactory, CommandFactory>();
        return services;
    }
}
