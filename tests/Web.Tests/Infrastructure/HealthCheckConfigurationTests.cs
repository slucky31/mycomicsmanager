using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Web.Configuration;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class HealthCheckConfigurationTests
{
    [Fact]
    public void AddAppHealthChecks_Should_KeepExternalApisOutOfLiveChecks()
    {
        var services = new ServiceCollection();

        services.AddAppHealthChecks("Host=localhost;Database=mcm");

        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        registrations.Select(r => r.Name).Should().Contain(["cloudinary", "debrid-link", "miniflux", "import-directory"]);
        registrations.Where(HealthCheckConfiguration.IsLiveCheck).Select(r => r.Name)
            .Should().Contain("import-directory")
            .And.NotContain(["cloudinary", "debrid-link", "miniflux"]);
    }

    [Fact]
    public async Task MapAppHealthChecks_Should_MapFullAndLiveEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        await using var app = builder.Build();

        app.MapAppHealthChecks();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(d => d.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText);
        routes.Should().Contain(["/health", "/health/live"]);
    }
}
