using Application.Libraries;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Persistence;
using Persistence.LocalStorage;

// Source : https://www.youtube.com/watch?v=tj5ZCtvgXKY&t=358s
// Source 2 : https://stackoverflow.com/questions/69990675/change-config-values-in-appsettings-json-githubactions

namespace Base.Integration.Tests;
#pragma warning disable CA1063 // Implement IDisposable Correctly
public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IDisposable
#pragma warning restore CA1063 // Implement IDisposable Correctly
{
    private string _connectionString = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Guard.Against.Null(builder);
        builder.ConfigureAppConfiguration((_, conf) =>
        {
            // Expand default config      
            conf.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true);

            // Add environment variables to override the parameters 
            conf.AddEnvironmentVariables();

            var configuration = conf.Build();

            _connectionString = configuration.GetConnectionString("NeonConnectionUnitTests") ?? string.Empty;
            Guard.Against.NullOrEmpty(_connectionString);
        });

        // Reconfigure the services to use the database with a new connection string       
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Add connection timeout to prevent hanging tests
            // Increased timeout for Neon database wake-up (free tier auto-pauses after inactivity)
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_connectionString)
            {
                Timeout = 60, // 60 seconds connection timeout (allows time for Neon to wake up)
                CommandTimeout = 60 // 60 seconds command timeout
            };

            services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(connectionStringBuilder.ToString(), npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly("Persistence")
                )
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(false)
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

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        
        try
        {
            // Run migrations after host is created to avoid deadlocks
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<IntegrationTestWebAppFactory>>();
                
                try
                {                    
                    db.Database.Migrate();
                    
                    logger.LogInformation("Database migrations completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to run database migrations");
                    throw new InvalidOperationException(
                        $"Cannot initialize test database: {ex.Message}\n" +
                        $"Connection: {MaskPassword(_connectionString)}\n" +
                        $"Ensure the database server is running and accessible.\n" +
                        $"Set ConnectionStrings__NeonConnectionUnitTests environment variable to use a local database.",
                        ex
                    );
                }
            }
            
            return host;
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }

    public new void Dispose()
    {
        base.Dispose();
    }

    private static string MaskPassword(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = "***MASKED***";
            }
            return builder.ToString();
        }
        catch (ArgumentException)
        {
            return "***CONNECTION_STRING_PARSE_ERROR***";
        }
    }

    private static string GetHostFromConnectionString(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            return builder.Host ?? "unknown";
        }
        catch (ArgumentException)
        {
            return "unknown";
        }
    }
}

