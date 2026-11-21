using Application.FeedImports;
using Application.FeedImports.Retry;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class RetryWaitingFeedImportDecisionsCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly DownloadCandidate s_candidate = new("Blacksad T03", "Blacksad T03.cbz", null,
        [new DownloadMirror("https://ddl.to/a", "ddl.to")]);

    private readonly IFeedImportDecisionRepository _repository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly IDebridLinkClient _debridLink = Substitute.For<IDebridLinkClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RetryWaitingFeedImportDecisionsCommandHandler _handler;
    private readonly CountRetryableFeedImportDecisionsQueryHandler _countHandler;

    public RetryWaitingFeedImportDecisionsCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success<int>(1));
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>()).Returns([]);
        GivenDdl(isUp: true);
        _handler = new RetryWaitingFeedImportDecisionsCommandHandler(
            _repository, _bookReadService, _debridLink, _unitOfWork, NullLogger<RetryWaitingFeedImportDecisionsCommandHandler>.Instance, TimeProvider.System);
        _countHandler = new CountRetryableFeedImportDecisionsQueryHandler(_repository, _debridLink, TimeProvider.System);
    }

    private void GivenDdl(bool isUp) =>
        _debridLink.GetHostsAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<DebridLinkHost>>([new DebridLinkHost("ddl", isUp, ["ddl.to", "ddownload.com"])]));

    private static FeedImportDecision Failed(string step, string message)
    {
        var decision = FeedImportDecision.Create(s_userId, 9, "Blacksad - Tome 3", "https://planete-bd.org/b3", null).Value!;
        decision.RecordLinks([s_candidate], new ParsedComicTitle("Blacksad", null, 3), "1 link", FeedImportDecidedBy.Auto);
        decision.StartDownload();
        decision.Fail(step, message);
        return decision;
    }

    private void GivenFailed(params FeedImportDecision[] decisions) =>
        _repository.GetByStatusAsync(s_userId, FeedImportDecisionStatus.Failed, Arg.Any<CancellationToken>()).Returns(decisions);

    private Task<Result<IReadOnlyList<RetriedFeedImportDecision>>> HandleAsync(FeedImportDecidedBy decidedBy) =>
        _handler.Handle(new RetryWaitingFeedImportDecisionsCommand(s_userId, decidedBy), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_RetryAndDownloadNext_WhenTheHostIsBackUp()
    {
        var waiting = Failed("Host", "Host down on Debrid-Link. (ddl.to)");
        var dead = Failed("Download", "File unavailable on the host (dead or deleted link).");
        GivenFailed(waiting, dead);

        var result = await HandleAsync(FeedImportDecidedBy.Auto);

        result.Value.Should().ContainSingle().Which.Should().Be(new RetriedFeedImportDecision(waiting.Id, FeedImportDecisionStatus.LinksExtracted));
        waiting.AutoRetryCount.Should().Be(1);
        dead.Status.Should().Be(FeedImportDecisionStatus.Failed);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RetryNothing_WhenTheHostIsStillDown()
    {
        GivenDdl(isUp: false);
        var waiting = Failed("Host", "Host down on Debrid-Link. (ddl.to)");
        GivenFailed(waiting);

        var result = await HandleAsync(FeedImportDecidedBy.Auto);

        result.Value.Should().BeEmpty();
        waiting.Status.Should().Be(FeedImportDecisionStatus.Failed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotReadTheHosts_WhenOnlyQuotaFailuresWait()
    {
        GivenFailed(Failed("Download", "Debrid-Link quota reached."));

        await HandleAsync(FeedImportDecidedBy.Auto);

        await _debridLink.DidNotReceive().GetHostsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_IgnoreTheAutomaticLimit_WhenTheUserRetries()
    {
        var waiting = Failed("Host", "Host down on Debrid-Link. (ddl.to)");
        for (var i = 0; i < FeedImportConstants.MaxAutoRetries; i++)
        {
            waiting.RetryAutomatically(_ => true);
            waiting.RecordLinks([s_candidate], new ParsedComicTitle("Blacksad", null, 3), "1 link", FeedImportDecidedBy.Auto);
            waiting.StartDownload();
            waiting.Fail("Host", "Host down on Debrid-Link. (ddl.to)");
        }
        GivenFailed(waiting);

        (await HandleAsync(FeedImportDecidedBy.Auto)).Value.Should().BeEmpty();
        var byUser = await HandleAsync(FeedImportDecidedBy.User);

        byUser.Value.Should().ContainSingle();
        waiting.DecidedBy.Should().Be(FeedImportDecidedBy.User);
        waiting.AutoRetryCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheSaveError_WhenSavingFails()
    {
        GivenFailed(Failed("Host", "Host down on Debrid-Link. (ddl.to)"));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure<int>(new TError("DB", "down")));

        var result = await HandleAsync(FeedImportDecidedBy.Auto);

        result.Error!.Code.Should().Be("DB");
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheUserIsEmpty()
    {
        var result = await _handler.Handle(new RetryWaitingFeedImportDecisionsCommand(Guid.Empty, FeedImportDecidedBy.Auto), TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.BadRequest);
        await _repository.DidNotReceive().GetByStatusAsync(Arg.Any<Guid>(), Arg.Any<FeedImportDecisionStatus>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CountHandle_Should_CountTheDecisionsTheButtonWouldRetry()
    {
        GivenFailed(
            Failed("Host", "Host down on Debrid-Link. (ddl.to)"),
            Failed("Download", "Debrid-Link quota reached."),
            Failed("Download", "File unavailable on the host (dead or deleted link)."));

        var result = await _countHandler.Handle(new CountRetryableFeedImportDecisionsQuery(s_userId), TestContext.Current.CancellationToken);

        // The quota failure has just happened: its delay is not over.
        result.Value.Should().Be(1);
    }

    [Fact]
    public async Task CountHandle_Should_CountNothing_WhenTheHostStatesAreUnavailable()
    {
        _debridLink.GetHostsAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<DebridLinkHost>>(FeedImportError.DebridLinkUnavailable));
        GivenFailed(Failed("Host", "Host down on Debrid-Link. (ddl.to)"));

        var result = await _countHandler.Handle(new CountRetryableFeedImportDecisionsQuery(s_userId), TestContext.Current.CancellationToken);

        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task CountHandle_Should_ReturnBadRequest_WhenTheUserIsEmpty()
    {
        var result = await _countHandler.Handle(new CountRetryableFeedImportDecisionsQuery(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.BadRequest);
    }
}
