using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using WebAPI.Options;

// Source : https://www.youtube.com/watch?v=tj5ZCtvgXKY&t=358s
// Source 2 : https://stackoverflow.com/questions/69990675/change-config-values-in-appsettings-json-githubactions

namespace Application.IntegrationTests;
public class IntegrationTestWebAppFactory:WebApplicationFactory<Program>, IAsyncLifetime
{
    private IConfiguration? _configuration;
    private MongoDbOptions? _mongoDbOptions;

    public Task InitializeAsync()
    {
        // TODO : delete previous tests db
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        builder.ConfigureAppConfiguration((context, conf) =>
        {
            // expand default config
            conf.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
            conf.AddEnvironmentVariables();

            // here we can "compile" the settings. Api.Setup will do the same, it doesn't matter.
            _configuration = conf.Build();

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

            var databaseName = _mongoDbOptions.DatabaseName + "_tests_" + DateTimeOffset.Now.ToString("yyyyMMddHHmmss");

            services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseMongoDB(_mongoDbOptions.ConnectionString, databaseName)
                .EnableDetailedErrors(true)
            );
        });
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
