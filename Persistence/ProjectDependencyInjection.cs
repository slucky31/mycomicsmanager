using Application.Data;
using Application.Interfaces;
using Application.Libraries;
using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Persistence.Queries;
using Persistence.Repositories;

namespace Persistence;

public static class ProjectDependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string dataBaseName)
    {
        var assembly = typeof(ProjectDependencyInjection).Assembly;

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(assembly));

        services.AddDbContext<ApplicationDbContext>(options => 
            options
                .UseMongoDB(connectionString, dataBaseName)
                .EnableDetailedErrors(true)
        );        

        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());

        services.AddScoped<IRepository<Library, ObjectId>, LibraryRepository>();

        services.AddScoped<ILibraryReadService, LibraryReadService>();

        return services;
    }
}
