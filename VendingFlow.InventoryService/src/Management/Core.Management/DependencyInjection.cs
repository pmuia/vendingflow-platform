using System.Reflection;
using InventoryService.Application.Common.Interfaces;
using InventoryService.Application.Common.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddMediatR(Assembly.GetExecutingAssembly());
        services.AddScoped<ICommandFactory, CommandFactory>();
        return services;
    }
}
