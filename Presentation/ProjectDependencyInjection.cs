using Microsoft.Extensions.DependencyInjection;

namespace Presentation;

public static class ProjectDependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        return services;
    }
}
