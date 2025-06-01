using Application.Libraries;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Persistence.LocalStorage;

// Source : https://www.youtube.com/watch?v=tj5ZCtvgXKY&t=358s
// Source 2 : https://stackoverflow.com/questions/69990675/change-config-values-in-appsettings-json-githubactions

namespace Base.Integration.Tests;
#pragma warning disable CA1063 // Implement IDisposable Correctly
public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IDisposable
#pragma warning restore CA1063 // Implement IDisposable Correctly
{
    private String _connectionString = String.Empty;

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

            var _configuration = conf.Build();

            _connectionString = _configuration.GetConnectionString("NeonConnectionUnitTests") ?? String.Empty;
        });

        // Reconfigure the services to use the MongoDb with a new database name        
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(_connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly("Persistence")
                )
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
        base.Dispose();
    }
}
