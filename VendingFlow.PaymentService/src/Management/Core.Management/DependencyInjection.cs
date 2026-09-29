using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Common.Services;

namespace PaymentService.Application;

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
