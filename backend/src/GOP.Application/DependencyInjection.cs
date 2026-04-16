using FluentValidation;
using GOP.Application.Common.Behaviors;
using GOP.Domain.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GOP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));

        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        services.AddAutoMapper(typeof(AssemblyMarker).Assembly);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
