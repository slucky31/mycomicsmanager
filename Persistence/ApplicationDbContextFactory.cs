using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Persistence;
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = "Development"; // valeur par défaut

        // Récupérer l'argument --environment passé à dotnet ef
        if (args != null && args.Length == 2 && args[0] == "--environment")
        {
            environment = args[1];
        }

        // Load configuration from appsettings.json and environment-specific files
        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../Web"));

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("NeonConnection");

        Log.Information("Using connection string: {ConnectionString}", connectionString);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

