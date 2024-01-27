using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Options;
using MongoDB.Driver;
using System.Globalization;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.TestHost;
using Persistence;

// Source : https://www.youtube.com/watch?v=tj5ZCtvgXKY&t=358s
// Source 2 : https://stackoverflow.com/questions/69990675/change-config-values-in-appsettings-json-githubactions

namespace Base.Integration.Tests;
public sealed class IntegrationTestWebAppFactory:WebApplicationFactory<Program>, IAsyncLifetime
{
    private MongoDbOptions? _mongoDbOptions;
    private string? _databaseName;

    Task IAsyncLifetime.InitializeAsync()
    {
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Guard.Against.Null(builder);
        builder.ConfigureAppConfiguration((_, conf) =>
        {
            // expand default config      

            conf.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json");

            conf.AddEnvironmentVariables();

            // here we can "compile" the settings. Api.Setup will do the same, it doesn't matter.
            var _configuration = conf.Build();

            _mongoDbOptions = _configuration.GetSection(nameof(MongoDbOptions)).Get<MongoDbOptions>();
        });

        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(s=> s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }
            
            Guard.Against.Null(_mongoDbOptions);
            _databaseName = _mongoDbOptions.DatabaseName + "_tests_" + DateTimeOffset.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseMongoDB(_mongoDbOptions.ConnectionString, _databaseName)
                .EnableDetailedErrors(true)
            );
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {        
        Guard.Against.Null(_mongoDbOptions);
        var client = new MongoClient(_mongoDbOptions.ConnectionString);
        await client.DropDatabaseAsync(_databaseName);       
    }
}
