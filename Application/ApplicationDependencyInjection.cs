using Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationDependencyInjection).Assembly;

        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false).AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false).AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false).AsImplementedInterfaces().WithScopedLifetime()
        );

        return services;
    }
}
