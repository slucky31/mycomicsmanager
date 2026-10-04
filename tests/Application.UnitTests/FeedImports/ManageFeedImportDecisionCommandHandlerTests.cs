using Application.Books;
using Application.FeedImports.Manage;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class ManageFeedImportDecisionCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly ParsedComicTitle s_parsed = new("Blacksad", "Âme rouge", 3);
    private static readonly DownloadCandidate s_candidate = new("Blacksad T03", "Blacksad T03.cbz", null,
        [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);

    private readonly IFeedImportDecisionRepository _repository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ManageFeedImportDecisionCommandHandler _handler;

    public ManageFeedImportDecisionCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>()).Returns([]);
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _handler = new ManageFeedImportDecisionCommandHandler(_repository, _bookReadService, _unitOfWork, _timeProvider);
    }

    private FeedImportDecision Given(Action<FeedImportDecision> arrange)
    {
        var decision = FeedImportDecision.Create(s_userId, 9, "Blacksad - Tome 3", "https://planete-bd.org/b3", null).Value!;
        arrange(decision);
        _repository.GetByIdAsync(decision.Id, Arg.Any<CancellationToken>()).Returns(decision);
        return decision;
    }

    private static void SkippedAsDuplicate(FeedImportDecision d) =>
        d.MarkDuplicate([s_candidate], s_parsed, Guid.CreateVersion7(), "Doublon", FeedImportDecidedBy.Auto);

    private static void FailedDownload(FeedImportDecision d)
    {
        d.RecordLinks([s_candidate], s_parsed, "1 lien", FeedImportDecidedBy.Auto);
        d.StartDownload();
        d.Fail("Téléchargement", "Quota Debrid-Link atteint.");
    }

    private Task<Result<FeedImportDecisionStatus>> HandleAsync(FeedImportDecision decision, FeedImportDecisionAction action, Guid? userId = null) =>
        _handler.Handle(new ManageFeedImportDecisionCommand(decision.Id, userId ?? s_userId, action), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenDecisionBelongsToAnotherUser()
    {
        var decision = Given(SkippedAsDuplicate);

        var result = await HandleAsync(decision, FeedImportDecisionAction.Ignore, Guid.CreateVersion7());

        result.Error.Should().Be(FeedImportError.NotFound);
        decision.Status.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenActionIsUnknown()
    {
        var result = await _handler.Handle(
            new ManageFeedImportDecisionCommand(Guid.CreateVersion7(), s_userId, (FeedImportDecisionAction)42), TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.BadRequest);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ForceDownloadAndReturnLinksExtracted_WhenSkippedAsDuplicate()
    {
        var decision = Given(SkippedAsDuplicate);

        var result = await HandleAsync(decision, FeedImportDecisionAction.ForceDownload);

        result.Value.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnErrorWithoutSaving_WhenTransitionIsNotAllowed()
    {
        var decision = Given(d => d.RecordLinks([s_candidate], s_parsed, "1 lien", FeedImportDecidedBy.Auto));

        var result = await HandleAsync(decision, FeedImportDecisionAction.ForceDownload);

        result.Error.Should().Be(FeedImportError.InvalidStatusTransition);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_IgnoreDecision_WhenUserIgnoresIt()
    {
        var decision = Given(SkippedAsDuplicate);

        var result = await HandleAsync(decision, FeedImportDecisionAction.Ignore);

        result.Value.Should().Be(FeedImportDecisionStatus.Ignored);
    }

    [Fact]
    public async Task Handle_Should_RetryFailedDownloadAndCheckDuplicatesAgain_WhenUserRetries()
    {
        var decision = Given(FailedDownload);

        var result = await HandleAsync(decision, FeedImportDecisionAction.Retry);

        result.Value.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.ErrorMessage.Should().BeNull();
        decision.ParsedSerie.Should().Be("Blacksad");
        await _bookReadService.Received(1).ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipRetriedDecision_WhenBookWasImportedMeanwhile()
    {
        var bookId = Guid.CreateVersion7();
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>())
            .Returns([new BookIdentityDto(bookId, "Blacksad", "Âme rouge", 3, null, "BD")]);
        var decision = Given(FailedDownload);

        var result = await HandleAsync(decision, FeedImportDecisionAction.Retry);

        result.Value.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.MatchedBookId.Should().Be(bookId);
    }

    [Fact]
    public async Task Handle_Should_LeaveRetriedArticlePendingForAnalysis_WhenItHasNoLinks()
    {
        var decision = Given(d => d.Fail("Récupération de la page", "Page indisponible."));

        var result = await HandleAsync(decision, FeedImportDecisionAction.Retry);

        result.Value.Should().Be(FeedImportDecisionStatus.Pending);
    }

    [Fact]
    public async Task Handle_Should_AskArbitrationAgain_WhenRetriedDecisionHasSeveralFiles()
    {
        var decision = Given(d =>
        {
            d.RequestArbitration(FeedImportArbitrationKind.AmbiguousLinks, [s_candidate, s_candidate with { FileName = "Autre.cbz" }],
                s_parsed, null, "Ambigu", FeedImportDecidedBy.Auto);
            d.Ignore();
        });

        var result = await HandleAsync(decision, FeedImportDecisionAction.Retry);

        result.Value.Should().Be(FeedImportDecisionStatus.AwaitingArbitration);
        decision.ArbitrationKind.Should().Be(FeedImportArbitrationKind.AmbiguousLinks);
    }

    [Fact]
    public async Task Handle_Should_RetryInterruptedDownload_OnlyWhenItIsStale()
    {
        var decision = Given(d =>
        {
            d.RecordLinks([s_candidate], s_parsed, "1 lien", FeedImportDecidedBy.Auto);
            d.StartDownload();
        });

        (await HandleAsync(decision, FeedImportDecisionAction.Retry)).Error.Should().Be(FeedImportError.InvalidStatusTransition);

        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow + FeedImportConstants.StaleDownloadDelay + TimeSpan.FromMinutes(1));
        (await HandleAsync(decision, FeedImportDecisionAction.Retry)).Value.Should().Be(FeedImportDecisionStatus.LinksExtracted);
    }
}
