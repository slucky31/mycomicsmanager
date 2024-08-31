using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ProjectDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {

        var assembly = typeof(ProjectDependencyInjection).Assembly;

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(assembly));

        return services;
    }
}
