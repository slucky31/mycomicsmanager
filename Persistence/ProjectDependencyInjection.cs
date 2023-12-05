using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Persistence;

public static class ProjectDependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string dataBaseName)
    {            
        var mongoDataBase = new MongoClient(connectionString).GetDatabase(dataBaseName);

        // TODO
        ApplicationDbContext.Create(mongoDataBase);

        return services;
    }
}
