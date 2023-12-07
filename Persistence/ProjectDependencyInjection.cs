using System.Net.Sockets;
using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Persistence.Repositories;

namespace Persistence;

public static class ProjectDependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string dataBaseName)
    {

        services.AddDbContext<ApplicationDbContext>(
            options => options.UseMongoDB(connectionString, dataBaseName));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());

        services.AddScoped<IRepository<Library, LibraryId>, Repository<Library, LibraryId>>();

        return services;
    }
}
