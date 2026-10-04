using Application.Books;
using Application.FeedImports.Manage;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class CorrectFeedImportDecisionCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Guid s_bookId = Guid.CreateVersion7();
    private static readonly DownloadCandidate s_candidate = new("Blacksad T03", "Blacksad T03.cbz", null,
        [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);

    private readonly IFeedImportDecisionRepository _repository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CorrectFeedImportDecisionCommandHandler _handler;

    public CorrectFeedImportDecisionCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>())
            .Returns([new BookIdentityDto(s_bookId, "Blacksad", "Âme rouge", 3, null, "BD")]);
        _handler = new CorrectFeedImportDecisionCommandHandler(_repository, _bookReadService, _unitOfWork);
    }

    private FeedImportDecision GivenLinksExtracted(ParsedComicTitle parsed)
    {
        var decision = FeedImportDecision.Create(s_userId, 9, "Blacksad 3", "https://planete-bd.org/b3", null).Value!;
        decision.RecordLinks([s_candidate], parsed, "1 lien", FeedImportDecidedBy.Auto);
        _repository.GetByIdAsync(decision.Id, Arg.Any<CancellationToken>()).Returns(decision);
        return decision;
    }

    private Task<Result<FeedImportDecisionStatus>> HandleAsync(Guid decisionId, string serie, string? title, int? volume) =>
        _handler.Handle(new CorrectFeedImportDecisionCommand(decisionId, s_userId, serie, title, volume), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_DetectDuplicate_WhenCorrectedTitleMatchesAnExistingBook()
    {
        // The parser missed the volume: no duplicate was found and the book would have been downloaded again.
        var decision = GivenLinksExtracted(new ParsedComicTitle("Blacksad 3", null, null));

        var result = await HandleAsync(decision.Id, " Blacksad ", " ", 3);

        result.Value.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.MatchedBookId.Should().Be(s_bookId);
        decision.ParsedSerie.Should().Be("Blacksad");
        decision.ParsedTitle.Should().BeNull();
        decision.DecidedBy.Should().Be(FeedImportDecidedBy.User);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ExtractLinks_WhenCorrectedTitleHasNoDuplicate()
    {
        var decision = GivenLinksExtracted(new ParsedComicTitle("Blacksad", null, 3));
        decision.Ignore();

        var result = await HandleAsync(decision.Id, "Blacksad", null, 4);

        result.Value.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.ParsedVolume.Should().Be(4);
    }

    [Theory]
    [InlineData("", 3)]
    [InlineData("Blacksad", -1)]
    public async Task Handle_Should_ReturnBadRequestWithoutLoading_WhenInputIsInvalid(string serie, int volume)
    {
        var result = await HandleAsync(Guid.CreateVersion7(), serie, null, volume);

        result.Error.Should().Be(FeedImportError.BadRequest);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenDecisionDoesNotExist()
    {
        var result = await HandleAsync(Guid.CreateVersion7(), "Blacksad", null, 3);

        result.Error.Should().Be(FeedImportError.NotFound);
    }
}
