using Application.FeedImports;
using AwesomeAssertions;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class MinifluxHealthCheckTests
{
    private readonly IMinifluxClient _client = Substitute.For<IMinifluxClient>();

    private MinifluxHealthCheck Build(bool enabled = true) => new(
        _client,
        Options.Create(new FeedImportSettings { Enabled = enabled }),
        Options.Create(new MinifluxSettings { CategoryName = "BD" }),
        new HealthCheckResultCache<MinifluxHealthCheck>());

    private void CategoriesAre(params string[] titles)
    {
        IReadOnlyList<MinifluxCategory> categories = titles.Select((t, i) => new MinifluxCategory(i + 1, t)).ToList();
        _client.GetCategoriesAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<MinifluxCategory>>.Success(categories));
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthy_WhenConfiguredCategoryExists()
    {
        CategoriesAre("News", " bd ");

        var result = await Build().CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnDegraded_WhenConfiguredCategoryIsMissing()
    {
        CategoriesAre("News");

        var result = await Build().CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("'BD'");
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnDegraded_WhenMinifluxIsUnavailable()
    {
        _client.GetCategoriesAsync(Arg.Any<CancellationToken>()).Returns(FeedImportError.MinifluxUnavailable);

        var result = await Build().CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain(FeedImportError.MinifluxUnavailable.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnDegraded_WhenApiKeyIsRejected()
    {
        _client.GetCategoriesAsync(Arg.Any<CancellationToken>()).Returns(FeedImportError.MinifluxUnauthorized);

        var result = await Build().CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain(FeedImportError.MinifluxUnauthorized.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnHealthyWithoutCallingApi_WhenFeedImportIsDisabled()
    {
        var result = await Build(enabled: false).CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
        await _client.DidNotReceive().GetCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_CacheResult_WhenCalledTwiceInQuickSuccession()
    {
        CategoriesAre("BD");
        var sut = Build();

        await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
        await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        await _client.Received(1).GetCategoriesAsync(Arg.Any<CancellationToken>());
    }
}
