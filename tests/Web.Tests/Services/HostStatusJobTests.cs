using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.HostStatus;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using Domain.Settings;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class HostStatusJobTests
{
    private readonly ICommandHandler<RecordHostStatusesCommand, RecordHostStatusesResult> _handler =
        Substitute.For<ICommandHandler<RecordHostStatusesCommand, RecordHostStatusesResult>>();
    private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    private readonly IFeatureToggles _featureToggles = Substitute.For<IFeatureToggles>();
    private readonly DebridLinkSettings _debridLinkSettings = new() { ApiKey = "key" };
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    public HostStatusJobTests()
    {
        _featureToggles.IsEnabled(FeatureToggle.FeedImport).Returns(true);
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<RecordHostStatusesCommand, RecordHostStatusesResult>)).Returns(_handler);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateScope().Returns(scope);
    }

    private HostStatusJob CreateJob() =>
        new(_scopeFactory, _backgroundJobClient, _featureToggles, Options.Create(_debridLinkSettings), NullLogger<HostStatusJob>.Instance);

    private void GivenRecorded(params string[] cameBackUp) =>
        _handler.Handle(Arg.Any<RecordHostStatusesCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RecordHostStatusesResult(cameBackUp.Length, cameBackUp, 0));

    [Fact]
    public async Task RecordAsync_Should_StartASync_WhenAHostIsBackUp()
    {
        GivenRecorded("ddl");

        await CreateJob().RecordAsync(TestContext.Current.CancellationToken);

        _backgroundJobClient.Received(1).Create(Arg.Is<Job>(j => j.Type == typeof(FeedImportSyncJob)), Arg.Any<IState>());
    }

    [Fact]
    public async Task RecordAsync_Should_NotStartASync_WhenNoHostCameBackUp()
    {
        GivenRecorded();

        await CreateJob().RecordAsync(TestContext.Current.CancellationToken);

        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task RecordAsync_Should_NotStartASync_WhenTheApiKeyIsNotSet()
    {
        _debridLinkSettings.ApiKey = string.Empty;
        GivenRecorded("ddl");

        await CreateJob().RecordAsync(TestContext.Current.CancellationToken);

        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task RecordAsync_Should_DoNothing_WhenFeedImportIsTurnedOff()
    {
        _featureToggles.IsEnabled(FeatureToggle.FeedImport).Returns(false);

        await CreateJob().RecordAsync(TestContext.Current.CancellationToken);

        await _handler.DidNotReceiveWithAnyArgs().Handle(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RecordAsync_Should_StopQuietly_WhenDebridLinkIsUnreachable()
    {
        _handler.Handle(Arg.Any<RecordHostStatusesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RecordHostStatusesResult>(FeedImportError.DebridLinkUnavailable));

        await CreateJob().RecordAsync(TestContext.Current.CancellationToken);

        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }
}
