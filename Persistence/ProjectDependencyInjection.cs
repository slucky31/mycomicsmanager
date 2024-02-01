using Application.Data;
using Application.Interfaces;
using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Persistence.Repositories;

namespace Persistence;

public static class ProjectDependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string dataBaseName)
    {        
        services.AddDbContext<ApplicationDbContext>(options => 
            options
                .UseMongoDB(connectionString, dataBaseName)
                .EnableDetailedErrors(true)
        );
        services.AddScoped<IApplicationDbContext>(sp => (IApplicationDbContext)sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());

        services.AddScoped<LibraryRepository>();
        services.AddScoped<IRepository<Library, ObjectId>>(sp => sp.GetRequiredService<LibraryRepository>());        

        return services;
    }
}
