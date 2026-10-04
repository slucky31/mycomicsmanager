using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.Arbitrate;
using Application.FeedImports.Delete;
using Application.FeedImports.List;
using Application.FeedImports.Manage;
using Application.Interfaces;
using Application.Libraries;
using AwesomeAssertions;
using Domain.FeedImports;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Users;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class FeedImportServiceTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IQueryHandler<GetPagedFeedImportDecisionsQuery, FeedImportDecisionPage> _handler;
    private readonly ICommandHandler<ResolveFeedImportArbitrationCommand> _resolveHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILibraryReadService _libraryReadService = Substitute.For<ILibraryReadService>();
    private readonly IFeedImportDecisionReadService _decisionReadService = Substitute.For<IFeedImportDecisionReadService>();
    private readonly ICommandHandler<ManageFeedImportDecisionCommand, FeedImportDecisionStatus> _manageHandler =
        Substitute.For<ICommandHandler<ManageFeedImportDecisionCommand, FeedImportDecisionStatus>>();
    private readonly ICommandHandler<CorrectFeedImportDecisionCommand, FeedImportDecisionStatus> _correctHandler =
        Substitute.For<ICommandHandler<CorrectFeedImportDecisionCommand, FeedImportDecisionStatus>>();
    private readonly ICommandHandler<DeleteFeedImportDecisionCommand> _deleteHandler =
        Substitute.For<ICommandHandler<DeleteFeedImportDecisionCommand>>();
    private readonly FeedImportSettings _settings = new() { Enabled = true };
    private readonly DebridLinkSettings _debridLinkSettings = new() { ApiKey = "key" };
    private readonly FeedImportService _service;

    public FeedImportServiceTests()
    {
        _handler = Substitute.For<IQueryHandler<GetPagedFeedImportDecisionsQuery, FeedImportDecisionPage>>();
        _resolveHandler = Substitute.For<ICommandHandler<ResolveFeedImportArbitrationCommand>>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(s_userId);
        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        _service = new FeedImportService(
            new FeedImportHandlers(_handler, _resolveHandler, _manageHandler, _correctHandler, _deleteHandler),
            _currentUserService, _libraryReadService, _decisionReadService, _backgroundJobClient,
            Options.Create(_settings), Options.Create(_debridLinkSettings), NullLogger<FeedImportService>.Instance);
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
    public async Task GetDecisionsAsync_Should_MapDecisionsAndLabelMultiBookArticles_WhenQuerySucceeds()
    {
        var multiBook = FeedImportDecision.Create(s_userId, 1, "Blacksad - Tomes 1 à 2", "https://planete-bd.org/1", null).Value!;
        var singleBook = FeedImportDecision.Create(s_userId, 2, "Blacksad T3", "https://planete-bd.org/2", null).Value!;
        var pagedList = Substitute.For<IPagedList<FeedImportDecision>>();
        pagedList.Items.Returns([multiBook, singleBook]);
        pagedList.TotalCount.Returns(41);
        _handler.Handle(new GetPagedFeedImportDecisionsQuery(s_userId, FeedImportDecisionStatus.Pending, "black", 3, 20), Arg.Any<CancellationToken>())
            .Returns(Result<FeedImportDecisionPage>.Success(new FeedImportDecisionPage(pagedList, new HashSet<long> { 1 })));

        var result = await _service.GetDecisionsAsync(FeedImportDecisionStatus.Pending, "black", 3, 20, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(41);
        result.Value.Items.Select(i => i.Id).Should().Equal(multiBook.Id, singleBook.Id);
        result.Value.Items[0].ItemDisplay.Should().Be("Book 1 of the article");
        result.Value.Items[1].ItemDisplay.Should().BeNull();
        await _libraryReadService.DidNotReceive().GetByNameAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDecisionsAsync_Should_LinkDownloadedDecisionsToImportPageOfTargetLibrary()
    {
        var decision = FeedImportDecision.Create(s_userId, 3, "Blacksad T3", "https://planete-bd.org/3", null).Value!;
        decision.RecordLinks(
            [new DownloadCandidate("Blacksad T03", "Blacksad T03.cbz", null, [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")])],
            ParsedComicTitle.Empty, "1 lien", FeedImportDecidedBy.Auto);
        decision.StartDownload();
        decision.MarkDownloaded("https://1fichier.com/?a", Guid.CreateVersion7(), "À trier");
        var pagedList = Substitute.For<IPagedList<FeedImportDecision>>();
        pagedList.Items.Returns([decision]);
        _handler.Handle(Arg.Any<GetPagedFeedImportDecisionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<FeedImportDecisionPage>.Success(new FeedImportDecisionPage(pagedList, new HashSet<long>())));
        var library = Library.Create("À trier", "#5C6BC0", "CollectionsBookmark", LibraryBookType.Digital, s_userId).Value!;
        _libraryReadService.GetByNameAsync(_settings.TargetLibraryName, s_userId, Arg.Any<CancellationToken>()).Returns(library);

        var result = await _service.GetDecisionsAsync(null, null, 1, 20, TestContext.Current.CancellationToken);

        result.Value!.Items.Should().ContainSingle().Which.ImportPageUrl.Should().Be($"/import?libraryId={library.Id}");
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
        AssertDownloadEnqueued(decisionId);
    }

    private void AssertDownloadEnqueued(Guid decisionId) =>
        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(j => j.Type == typeof(FeedImportDownloadJob) && (Guid)j.Args[0] == decisionId), Arg.Any<IState>());

    [Fact]
    public async Task ApplyActionAsync_Should_StartDownload_WhenDecisionEndsLinksExtracted()
    {
        var decisionId = Guid.CreateVersion7();
        _manageHandler.Handle(new ManageFeedImportDecisionCommand(decisionId, s_userId, FeedImportDecisionAction.ForceDownload), Arg.Any<CancellationToken>())
            .Returns(FeedImportDecisionStatus.LinksExtracted);

        var result = await _service.ApplyActionAsync(decisionId, FeedImportDecisionAction.ForceDownload, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        AssertDownloadEnqueued(decisionId);
    }

    [Fact]
    public async Task ApplyActionAsync_Should_NotStartDownload_WhenApiKeyIsMissing()
    {
        _debridLinkSettings.ApiKey = string.Empty;
        _manageHandler.Handle(Arg.Any<ManageFeedImportDecisionCommand>(), Arg.Any<CancellationToken>()).Returns(FeedImportDecisionStatus.LinksExtracted);

        await _service.ApplyActionAsync(Guid.CreateVersion7(), FeedImportDecisionAction.Retry, TestContext.Current.CancellationToken);

        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task ApplyActionAsync_Should_StartSync_WhenRetriedArticleMustBeAnalyzedAgain()
    {
        _manageHandler.Handle(Arg.Any<ManageFeedImportDecisionCommand>(), Arg.Any<CancellationToken>()).Returns(FeedImportDecisionStatus.Pending);

        await _service.ApplyActionAsync(Guid.CreateVersion7(), FeedImportDecisionAction.Retry, TestContext.Current.CancellationToken);

        _backgroundJobClient.Received(1).Create(Arg.Is<Job>(j => j.Type == typeof(FeedImportSyncJob)), Arg.Any<IState>());
    }

    [Fact]
    public async Task ApplyActionAsync_Should_ReturnErrorWithoutEnqueuing_WhenActionFails()
    {
        _manageHandler.Handle(Arg.Any<ManageFeedImportDecisionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<FeedImportDecisionStatus>.Failure(FeedImportError.InvalidStatusTransition));

        var result = await _service.ApplyActionAsync(Guid.CreateVersion7(), FeedImportDecisionAction.Ignore, TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.InvalidStatusTransition);
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task CorrectAsync_Should_ForwardValuesForCurrentUser()
    {
        var decisionId = Guid.CreateVersion7();
        _correctHandler.Handle(Arg.Any<CorrectFeedImportDecisionCommand>(), Arg.Any<CancellationToken>()).Returns(FeedImportDecisionStatus.SkippedDuplicate);

        var result = await _service.CorrectAsync(decisionId, "Blacksad", "Âme rouge", 3, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _correctHandler.Received(1).Handle(
            new CorrectFeedImportDecisionCommand(decisionId, s_userId, "Blacksad", "Âme rouge", 3), Arg.Any<CancellationToken>());
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task DeleteAsync_Should_DeleteEachDecisionForCurrentUser_AndCountDeleted()
    {
        var deletable = Guid.CreateVersion7();
        var inProgress = Guid.CreateVersion7();
        _deleteHandler.Handle(new DeleteFeedImportDecisionCommand(deletable, s_userId), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _deleteHandler.Handle(new DeleteFeedImportDecisionCommand(inProgress, s_userId), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(FeedImportError.DeleteInProgress));

        var result = await _service.DeleteAsync([deletable, inProgress], TestContext.Current.CancellationToken);

        result.Value.Should().Be(1);
        await _deleteHandler.Received(2).Handle(Arg.Any<DeleteFeedImportDecisionCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Should_ReturnError_WhenNoDecisionCouldBeDeleted()
    {
        _deleteHandler.Handle(Arg.Any<DeleteFeedImportDecisionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(FeedImportError.DeleteInProgress));

        var result = await _service.DeleteAsync([Guid.CreateVersion7()], TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.DeleteInProgress);
    }

    [Fact]
    public async Task DeleteAsync_Should_ReturnError_WhenUserNotResolved()
    {
        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));

        var result = await _service.DeleteAsync([Guid.CreateVersion7()], TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.NotFound);
        await _deleteHandler.DidNotReceiveWithAnyArgs().Handle(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CountAwaitingArbitrationAsync_Should_CountForCurrentUser_AndReturnZeroWhenUserIsUnknown()
    {
        _decisionReadService.CountByStatusAsync(s_userId, FeedImportDecisionStatus.AwaitingArbitration, Arg.Any<CancellationToken>()).Returns(3);

        (await _service.CountAwaitingArbitrationAsync(TestContext.Current.CancellationToken)).Should().Be(3);

        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>()).Returns(Result<Guid>.Failure(UsersError.NotFound));
        (await _service.CountAwaitingArbitrationAsync(TestContext.Current.CancellationToken)).Should().Be(0);
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
