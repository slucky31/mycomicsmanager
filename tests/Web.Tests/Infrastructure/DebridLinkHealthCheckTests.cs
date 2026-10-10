using Application.FeedImports;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.FeedImports;
using Domain.Primitives;
using Domain.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class DebridLinkHealthCheckTests
{
    private readonly IDebridLinkClient _client = Substitute.For<IDebridLinkClient>();

    private static IFeatureToggles FeatureToggles(bool enabled)
    {
        var featureToggles = Substitute.For<IFeatureToggles>();
        featureToggles.IsEnabled(FeatureToggle.FeedImport).Returns(enabled);
        return featureToggles;
    }

    private DebridLinkHealthCheck Build(bool enabled = true, string apiKey = "secret") => new(
        _client,
        FeatureToggles(enabled),
        Options.Create(new DebridLinkSettings { ApiKey = apiKey }),
        new HealthCheckResultCache<DebridLinkHealthCheck>());

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_WhenAccountCheckSucceeds()
    {
        _client.CheckAccountAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await Build().CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnDegraded_WhenApiKeyIsRejected()
    {
        _client.CheckAccountAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(FeedImportError.DebridLinkUnauthorized));

        var result = await Build().CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain(FeedImportError.DebridLinkUnauthorized.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnDegraded_WhenDebridLinkIsUnavailable()
    {
        _client.CheckAccountAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(FeedImportError.DebridLinkUnavailable));

        var result = await Build().CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain(FeedImportError.DebridLinkUnavailable.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnDegradedWithoutCallingApi_WhenApiKeyIsMissing()
    {
        var result = await Build(apiKey: " ").CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
        await _client.DidNotReceive().CheckAccountAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthyWithoutCallingApi_WhenFeedImportIsDisabled()
    {
        var result = await Build(enabled: false).CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
        await _client.DidNotReceive().CheckAccountAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_CacheResult_WhenCalledTwiceInQuickSuccession()
    {
        _client.CheckAccountAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        var sut = Build();

        await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
        await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        await _client.Received(1).CheckAccountAsync(Arg.Any<CancellationToken>());
    }
}
