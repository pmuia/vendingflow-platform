using MachineService.Application.Common.Interfaces;
using MachineService.Application.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MachineService.Application;

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
