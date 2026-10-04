using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.Analyze;
using Application.FeedImports.Sync;
using Application.Interfaces;
using Application.Users;
using AwesomeAssertions;
using Domain.FeedImports;
using Domain.Primitives;
using Domain.Users;
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

public sealed class FeedImportSyncJobTests
{
    private const string UserEmail = "reader@example.com";

    private readonly ICommandHandler<SyncFeedImportsCommand, SyncFeedImportsResult> _handler;
    private readonly IUserReadService _userReadService;
    private readonly IFeedImportDecisionRepository _decisionRepository;
    private readonly ICommandHandler<AnalyzeFeedImportDecisionCommand> _analyzeHandler;
    private readonly IBackgroundJobClient _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FeedImportSettings _settings = new() { Enabled = true, UserEmail = UserEmail };
    private readonly DebridLinkSettings _debridLinkSettings = new() { ApiKey = "key" };

    public FeedImportSyncJobTests()
    {
        _handler = Substitute.For<ICommandHandler<SyncFeedImportsCommand, SyncFeedImportsResult>>();
        _userReadService = Substitute.For<IUserReadService>();
        _decisionRepository = Substitute.For<IFeedImportDecisionRepository>();
        _decisionRepository.GetPendingIdsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _decisionRepository.GetIdsByStatusAsync(Arg.Any<Guid>(), Arg.Any<FeedImportDecisionStatus>(), Arg.Any<CancellationToken>()).Returns([]);
        _analyzeHandler = Substitute.For<ICommandHandler<AnalyzeFeedImportDecisionCommand>>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<SyncFeedImportsCommand, SyncFeedImportsResult>)).Returns(_handler);
        serviceProvider.GetService(typeof(IUserReadService)).Returns(_userReadService);
        serviceProvider.GetService(typeof(IFeedImportDecisionRepository)).Returns(_decisionRepository);
        serviceProvider.GetService(typeof(ICommandHandler<AnalyzeFeedImportDecisionCommand>)).Returns(_analyzeHandler);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(scope);
    }

    private FeedImportSyncJob CreateJob() =>
        new(_scopeFactory, _backgroundJobClient, Options.Create(_settings), Options.Create(_debridLinkSettings), NullLogger<FeedImportSyncJob>.Instance);

    private User ArrangeUserWithFailedSync()
    {
        var user = User.Create(UserEmail, "auth0|1");
        _userReadService.GetUserByEmail(UserEmail, Arg.Any<CancellationToken>()).Returns(user);
        _handler.Handle(Arg.Any<SyncFeedImportsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<SyncFeedImportsResult>.Failure(new TError("FEED502", "Miniflux down")));
        return user;
    }

    [Fact]
    public async Task SyncAsync_Should_EnqueueOneDownloadPerDecision_WhenApiKeyIsSet()
    {
        var user = ArrangeUserWithFailedSync();
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        _decisionRepository.GetIdsByStatusAsync(user.Id, FeedImportDecisionStatus.LinksExtracted, Arg.Any<CancellationToken>())
            .Returns([first, second]);

        await CreateJob().SyncAsync(TestContext.Current.CancellationToken);

        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(j => j.Type == typeof(FeedImportDownloadJob) && (Guid)j.Args[0] == first), Arg.Any<IState>());
        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(j => j.Type == typeof(FeedImportDownloadJob) && (Guid)j.Args[0] == second), Arg.Any<IState>());
    }

    [Fact]
    public async Task SyncAsync_Should_NotEnqueueDownloads_WhenApiKeyIsMissing()
    {
        var user = ArrangeUserWithFailedSync();
        _debridLinkSettings.ApiKey = string.Empty;
        _decisionRepository.GetIdsByStatusAsync(user.Id, FeedImportDecisionStatus.LinksExtracted, Arg.Any<CancellationToken>())
            .Returns([Guid.CreateVersion7()]);

        await CreateJob().SyncAsync(TestContext.Current.CancellationToken);

        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task SyncAsync_Should_RunSyncForConfiguredUser_WhenEnabled()
    {
        var user = User.Create(UserEmail, "auth0|1");
        _userReadService.GetUserByEmail(UserEmail, Arg.Any<CancellationToken>()).Returns(user);
        _handler.Handle(Arg.Any<SyncFeedImportsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new SyncFeedImportsResult(1, 1, 0, 0, 0));

        await CreateJob().SyncAsync(TestContext.Current.CancellationToken);

        await _handler.Received(1).Handle(new SyncFeedImportsCommand(user.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_Should_AnalyzeEachPendingDecision_EvenWhenOneThrows()
    {
        var user = User.Create(UserEmail, "auth0|1");
        _userReadService.GetUserByEmail(UserEmail, Arg.Any<CancellationToken>()).Returns(user);
        _handler.Handle(Arg.Any<SyncFeedImportsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<SyncFeedImportsResult>.Failure(new TError("FEED502", "Miniflux down")));
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        _decisionRepository.GetPendingIdsAsync(user.Id, Arg.Any<CancellationToken>()).Returns([first, second]);
        _analyzeHandler.Handle(new AnalyzeFeedImportDecisionCommand(first), Arg.Any<CancellationToken>())
            .Returns<Task<Result>>(_ => throw new InvalidOperationException("boom"));
        _analyzeHandler.Handle(new AnalyzeFeedImportDecisionCommand(second), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await CreateJob().SyncAsync(TestContext.Current.CancellationToken);

        await _analyzeHandler.Received(1).Handle(new AnalyzeFeedImportDecisionCommand(first), Arg.Any<CancellationToken>());
        await _analyzeHandler.Received(1).Handle(new AnalyzeFeedImportDecisionCommand(second), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_Should_DoNothing_WhenDisabled()
    {
        _settings.Enabled = false;

        await CreateJob().SyncAsync(TestContext.Current.CancellationToken);

        _scopeFactory.DidNotReceive().CreateScope();
    }

    [Fact]
    public async Task SyncAsync_Should_NotRunSync_WhenConfiguredUserIsUnknown()
    {
        _userReadService.GetUserByEmail(UserEmail, Arg.Any<CancellationToken>()).Returns(Result<User>.Failure(UsersError.NotFound));

        await CreateJob().SyncAsync(TestContext.Current.CancellationToken);

        await _handler.DidNotReceive().Handle(Arg.Any<SyncFeedImportsCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(59, true)]
    [InlineData(120, true)]
    [InlineData(1440, true)]
    [InlineData(0, false)]
    [InlineData(90, false)]
    [InlineData(1500, false)]
    public void IsValidInterval_Should_AcceptOnlyCronExpressibleIntervals(int minutes, bool expected)
    {
        FeedImportSyncJob.IsValidInterval(minutes).Should().Be(expected);
    }

    [Theory]
    [InlineData(30, "*/30 * * * *")]
    [InlineData(120, "0 */2 * * *")]
    public void ToCron_Should_BuildCronExpression(int minutes, string expected)
    {
        FeedImportSyncJob.ToCron(minutes).Should().Be(expected);
    }

    [Fact]
    public void Schedule_Should_RegisterRecurringJob_WhenEnabled()
    {
        var manager = Substitute.For<IRecurringJobManager>();

        FeedImportSyncJob.Schedule(manager, new FeedImportSettings { Enabled = true, SyncIntervalMinutes = 15 });

        manager.Received(1).AddOrUpdate(
            FeedImportSyncJob.RecurringJobId,
            Arg.Is<Job>(j => j.Type == typeof(FeedImportSyncJob) && j.Method.Name == nameof(FeedImportSyncJob.SyncAsync)),
            "*/15 * * * *",
            Arg.Any<RecurringJobOptions>());
        manager.DidNotReceive().RemoveIfExists(Arg.Any<string>());
    }

    [Fact]
    public void Schedule_Should_RemoveRecurringJob_WhenDisabled()
    {
        var manager = Substitute.For<IRecurringJobManager>();

        FeedImportSyncJob.Schedule(manager, new FeedImportSettings { Enabled = false });

        manager.Received(1).RemoveIfExists(FeedImportSyncJob.RecurringJobId);
    }
}
