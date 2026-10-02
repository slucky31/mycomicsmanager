using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.Sync;
using Application.Users;
using AwesomeAssertions;
using Domain.Primitives;
using Domain.Users;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FeedImportSettings _settings = new() { Enabled = true, UserEmail = UserEmail };

    public FeedImportSyncJobTests()
    {
        _handler = Substitute.For<ICommandHandler<SyncFeedImportsCommand, SyncFeedImportsResult>>();
        _userReadService = Substitute.For<IUserReadService>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandHandler<SyncFeedImportsCommand, SyncFeedImportsResult>)).Returns(_handler);
        serviceProvider.GetService(typeof(IUserReadService)).Returns(_userReadService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(scope);
    }

    private FeedImportSyncJob CreateJob() => new(_scopeFactory, Options.Create(_settings));

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
