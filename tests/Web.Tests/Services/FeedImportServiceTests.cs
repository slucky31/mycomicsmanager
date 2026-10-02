using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.Arbitrate;
using Application.FeedImports.List;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.FeedImports;
using Domain.Primitives;
using Domain.Users;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Options;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class FeedImportServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IQueryHandler<GetPagedFeedImportDecisionsQuery, IPagedList<FeedImportDecision>> _handler;
    private readonly ICommandHandler<ResolveFeedImportArbitrationCommand> _resolveHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly FeedImportSettings _settings = new() { Enabled = true };
    private readonly FeedImportService _service;

    public FeedImportServiceTests()
    {
        _handler = Substitute.For<IQueryHandler<GetPagedFeedImportDecisionsQuery, IPagedList<FeedImportDecision>>>();
        _resolveHandler = Substitute.For<ICommandHandler<ResolveFeedImportArbitrationCommand>>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        _service = new FeedImportService(_handler, _resolveHandler, _currentUserService, _backgroundJobClient, Options.Create(_settings));
    }

    [Fact]
    public async Task GetDecisionsAsync_Should_ReturnError_WhenUserNotResolved()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        var result = await _service.GetDecisionsAsync(null, null, 1, 20, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
        await _handler.DidNotReceive().Handle(Arg.Any<GetPagedFeedImportDecisionsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDecisionsAsync_Should_MapDecisionsToViewModels_WhenQuerySucceeds()
    {
        var decision = FeedImportDecision.Create(s_userId, 1, "Blacksad T3", "https://planete-bd.org/1", null).Value!;
        var pagedList = Substitute.For<IPagedList<FeedImportDecision>>();
        pagedList.Items.Returns([decision]);
        pagedList.TotalCount.Returns(41);
        _handler.Handle(new GetPagedFeedImportDecisionsQuery(s_userId, FeedImportDecisionStatus.Pending, "black", 3, 20), Arg.Any<CancellationToken>())
            .Returns(Result<IPagedList<FeedImportDecision>>.Success(pagedList));

        var result = await _service.GetDecisionsAsync(FeedImportDecisionStatus.Pending, "black", 3, 20, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(41);
        result.Value.Items.Should().ContainSingle().Which.Id.Should().Be(decision.Id);
    }

    [Fact]
    public void TriggerSync_Should_EnqueueSyncJob_WhenEnabled()
    {
        var result = _service.TriggerSync();

        result.IsSuccess.Should().BeTrue();
        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(j => j.Type == typeof(FeedImportSyncJob) && j.Method.Name == nameof(FeedImportSyncJob.SyncAsync)),
            Arg.Any<IState>());
    }

    [Fact]
    public void TriggerSync_Should_ReturnDisabled_WhenFeedImportIsDisabled()
    {
        _settings.Enabled = false;

        var result = _service.TriggerSync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.Disabled);
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task ResolveArbitrationAsync_Should_ForwardCurrentUser_WhenUserResolved()
    {
        var decisionId = Guid.CreateVersion7();
        _resolveHandler.Handle(Arg.Any<ResolveFeedImportArbitrationCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _service.ResolveArbitrationAsync(decisionId, FeedImportArbitrationAction.KeepCandidate, 2, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _resolveHandler.Received(1).Handle(
            new ResolveFeedImportArbitrationCommand(decisionId, s_userId, FeedImportArbitrationAction.KeepCandidate, 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveArbitrationAsync_Should_ReturnError_WhenUserNotResolved()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        var result = await _service.ResolveArbitrationAsync(Guid.CreateVersion7(), FeedImportArbitrationAction.NotDuplicate, null, TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.NotFound);
        await _resolveHandler.DidNotReceive().Handle(Arg.Any<ResolveFeedImportArbitrationCommand>(), Arg.Any<CancellationToken>());
    }
}
