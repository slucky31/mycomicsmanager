using Application.Books;
using Application.FeedImports.Arbitrate;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class ResolveFeedImportArbitrationCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly ParsedComicTitle s_parsed = new("Blacksad", null, 3);

    private static readonly DownloadCandidate s_first = new("Lien 1", null, null, [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);
    private static readonly DownloadCandidate s_second = new("Lien 2", "Blacksad_T03.cbz", 10, [new DownloadMirror("https://1fichier.com/?b", "1fichier.com")]);

    private readonly IFeedImportDecisionRepository _repository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResolveFeedImportArbitrationCommandHandler _handler;

    public ResolveFeedImportArbitrationCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>()).Returns([]);
        _handler = new ResolveFeedImportArbitrationCommandHandler(_repository, _bookReadService, _unitOfWork);
    }

    private FeedImportDecision GivenAwaiting(FeedImportArbitrationKind kind, Guid? matchedBookId = null)
    {
        var decision = FeedImportDecision.Create(s_userId, 9, "Blacksad - Tome 3", "https://planete-bd.org/b3", null).Value!;
        decision.RequestArbitration(kind, [s_first, s_second], s_parsed, matchedBookId, "À arbitrer", FeedImportDecidedBy.Auto);
        _repository.GetByIdAsync(decision.Id, Arg.Any<CancellationToken>()).Returns(decision);
        return decision;
    }

    private Task<Result> HandleAsync(FeedImportDecision decision, FeedImportArbitrationAction action, int? index = null, Guid? userId = null) =>
        _handler.Handle(new ResolveFeedImportArbitrationCommand(decision.Id, userId ?? s_userId, action, index), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenDecisionBelongsToAnotherUser()
    {
        var decision = GivenAwaiting(FeedImportArbitrationKind.AmbiguousLinks);

        var result = await HandleAsync(decision, FeedImportArbitrationAction.MergeAsMirrors, userId: Guid.CreateVersion7());

        result.Error.Should().Be(FeedImportError.NotFound);
        decision.Status.Should().Be(FeedImportDecisionStatus.AwaitingArbitration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public async Task Handle_Should_ReturnBadRequest_WhenKeepCandidateHasNoValidIndex(int? index)
    {
        var decision = GivenAwaiting(FeedImportArbitrationKind.AmbiguousLinks);

        var result = await HandleAsync(decision, FeedImportArbitrationAction.KeepCandidate, index);

        result.Error.Should().Be(FeedImportError.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_KeepOnlyChosenCandidate_WhenUserPicksOne()
    {
        var decision = GivenAwaiting(FeedImportArbitrationKind.AmbiguousLinks);

        var result = await HandleAsync(decision, FeedImportArbitrationAction.KeepCandidate, 1);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.DecidedBy.Should().Be(FeedImportDecidedBy.User);
        decision.GetCandidates().Should().ContainSingle().Which.Label.Should().Be("Lien 2");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_MergeAllLinksAsMirrorsAndCheckDuplicates_WhenUserMerges()
    {
        var decision = GivenAwaiting(FeedImportArbitrationKind.AmbiguousLinks);
        var existing = new BookIdentityDto(Guid.CreateVersion7(), "Blacksad", "Âme rouge", 3, null, "BD");
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>()).Returns([existing]);

        await HandleAsync(decision, FeedImportArbitrationAction.MergeAsMirrors);

        decision.Status.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.MatchedBookId.Should().Be(existing.Id);
        var candidate = decision.GetCandidates().Should().ContainSingle().Subject;
        candidate.Mirrors.Should().HaveCount(2);
        candidate.FileName.Should().Be("Blacksad_T03.cbz");
    }

    [Fact]
    public async Task Handle_Should_ConfirmNotDuplicate_WhenProbableDuplicateIsRejected()
    {
        var decision = GivenAwaiting(FeedImportArbitrationKind.ProbableDuplicate, Guid.CreateVersion7());

        await HandleAsync(decision, FeedImportArbitrationAction.NotDuplicate);

        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.MatchedBookId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidTransition_WhenChoosingLinksOnProbableDuplicate()
    {
        var decision = GivenAwaiting(FeedImportArbitrationKind.ProbableDuplicate, Guid.CreateVersion7());

        var result = await HandleAsync(decision, FeedImportArbitrationAction.MergeAsMirrors);

        result.Error.Should().Be(FeedImportError.InvalidStatusTransition);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
