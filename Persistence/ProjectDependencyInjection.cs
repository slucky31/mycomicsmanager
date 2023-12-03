using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Persistence;

public static class ProjectDependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        var connectionString = "mongodb+srv://api-rest-dev:cyKVB7Jc2oKsympb@dev.dvd91.azure.mongodb.net/";
        var dataBaseName = "Dev";

        var mongoDataBase = new MongoClient(connectionString).GetDatabase(dataBaseName);
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMongoDB(mongoDataBase.Client, mongoDataBase.DatabaseNamespace.DatabaseName);

        /*
        using (var db = new ApplicationDbContext(dbContextOptions.Options))
        {
            var library = Library.Create("Mongo");
            db.Libraries.Add(library);
            db.SaveChanges();
        }
        */
               
        return services;
    }
}
