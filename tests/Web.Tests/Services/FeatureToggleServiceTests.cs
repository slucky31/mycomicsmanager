using Application.Abstractions.Messaging;
using Application.Books.IsbnScan;
using Application.ComicInfoSearch;
using Application.FeedImports;
using Application.Settings;
using Application.Settings.SetFeatureToggle;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Settings;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class FeatureToggleServiceTests
{
    private readonly ICommandHandler<SetFeatureToggleCommand> _setHandler = Substitute.For<ICommandHandler<SetFeatureToggleCommand>>();
    private readonly IRecurringJobManager _recurringJobManager = Substitute.For<IRecurringJobManager>();
    private readonly FeedImportSettings _feedImportSettings = new() { Enabled = false, SyncIntervalMinutes = 30 };
    private readonly FeatureToggles _featureToggles;
    private readonly FeatureToggleService _service;

    public FeatureToggleServiceTests()
    {
        var defaults = new FeatureToggleDefaults(
            Options.Create(_feedImportSettings),
            Options.Create(new MinifluxSettings()),
            Options.Create(new IsbnOcrSettings()),
            Options.Create(new BedethequeSettings { BaseUrl = new Uri("https://www.bedetheque.com"), SerpApiBaseUrl = new Uri("https://serpapi.com") }));
        _featureToggles = new FeatureToggles(defaults);
        _service = new FeatureToggleService(_setHandler, _featureToggles, _recurringJobManager, Options.Create(_feedImportSettings),
            NullLogger<FeatureToggleService>.Instance);
    }

    [Fact]
    public void GetToggles_Should_ListEveryFeatureWithItsCurrentValue()
    {
        _featureToggles.Apply(FeatureToggle.IsbnOcr, false);

        var toggles = _service.GetToggles();

        toggles.Select(t => t.Toggle).Should().Equal(Enum.GetValues<FeatureToggle>());
        toggles.Single(t => t.Toggle == FeatureToggle.IsbnOcr).IsOverridden.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsync_Should_ScheduleTheMinifluxSync_WhenFeedImportIsTurnedOn()
    {
        _setHandler.Handle(new SetFeatureToggleCommand(FeatureToggle.FeedImport, true), Arg.Any<CancellationToken>())
            .Returns(Result.Success())
            .AndDoes(_ => _featureToggles.Apply(FeatureToggle.FeedImport, true));

        var result = await _service.SetAsync(FeatureToggle.FeedImport, true, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _recurringJobManager.Received(1).AddOrUpdate(FeedImportSyncJob.RecurringJobId, Arg.Any<Job>(), "*/30 * * * *", Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public async Task SetAsync_Should_RemoveTheMinifluxSync_WhenFeedImportIsTurnedOff()
    {
        _setHandler.Handle(Arg.Any<SetFeatureToggleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await _service.SetAsync(FeatureToggle.FeedImport, false, TestContext.Current.CancellationToken);

        _recurringJobManager.Received(1).RemoveIfExists(FeedImportSyncJob.RecurringJobId);
    }

    [Fact]
    public async Task SetAsync_Should_NotTouchHangfire_WhenAnotherFeatureChanges()
    {
        _setHandler.Handle(Arg.Any<SetFeatureToggleCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _service.SetAsync(FeatureToggle.Bedetheque, false, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _recurringJobManager.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task SetAsync_Should_ReturnError_AndNotTouchHangfire_WhenCommandFails()
    {
        _setHandler.Handle(Arg.Any<SetFeatureToggleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(FeatureToggleError.MissingConfiguration("Miniflux:ApiKey is required.")));

        var result = await _service.SetAsync(FeatureToggle.FeedImport, true, TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("TOGGLE409");
        _recurringJobManager.ReceivedCalls().Should().BeEmpty();
    }
}
