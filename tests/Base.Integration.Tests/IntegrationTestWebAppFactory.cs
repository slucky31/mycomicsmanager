using Application.Libraries;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Persistence;
using Persistence.LocalStorage;
using Web.Configuration;
using Persistence.LocalStorage;

// Source : https://www.youtube.com/watch?v=tj5ZCtvgXKY&t=358s
// Source 2 : https://stackoverflow.com/questions/69990675/change-config-values-in-appsettings-json-githubactions

namespace Base.Integration.Tests;
#pragma warning disable CA1063 // Implement IDisposable Correctly
public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IDisposable
#pragma warning restore CA1063 // Implement IDisposable Correctly
{
    private MongoDbOptions? _mongoDbOptions;    

    private string? _databaseName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Guard.Against.Null(builder);
        builder.ConfigureAppConfiguration((_, conf) =>
        {
            // Expand default config      
            conf.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true);

            conf.AddEnvironmentVariables();

            // here we can "compile" the settings. Api.Setup will do the same, it doesn't matter.
            var _configuration = conf.Build();

            _mongoDbOptions = _configuration.GetSection(nameof(MongoDbOptions)).Get<MongoDbOptions>();            
        });

        // Reconfigure the services to use the MongoDb with a new database name        
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            Guard.Against.Null(_mongoDbOptions);
            _databaseName = Guid.NewGuid().ToString();

            services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseMongoDB(_mongoDbOptions.ConnectionString, _databaseName)
                .EnableDetailedErrors(true)
            );
        });

        // Reconfigure the service to use LocalStorage with a new root path as Temp directory
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(ILibraryLocalStorage));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }
            
            var rootPath = Path.GetTempPath();
            services.AddScoped<ILibraryLocalStorage>(provider => new LibraryLocalStorage(rootPath));
        });
    }

    public new void Dispose()
    {
        Guard.Against.Null(_mongoDbOptions);
        using (var client = new MongoClient(_mongoDbOptions.ConnectionString))
        {
            client.DropDatabase(_databaseName);
        }
        base.Dispose();
    }
}
