using Application.Books;
using Application.FeedImports;
using Application.FeedImports.Analysis;
using Application.FeedImports.Analysis.Sites;
using Application.FeedImports.Analyze;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class AnalyzeFeedImportDecisionCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IFeedImportDecisionRepository _repository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IArticlePageFetcher _pageFetcher = Substitute.For<IArticlePageFetcher>();
    private readonly IBookReadService _bookReadService = Substitute.For<IBookReadService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FeedImportSettings _settings = new()
    {
        AllowedSourceHosts = ["planete-bd.org", "zone-ebook.com"],
        AllowedDownloadHosts = ["1fichier.com", "rapidgator.net", "fileq.net", "dailyuploads.net", "frdl.io", "katfile.biz", "trbt.cc"]
    };
    private readonly AnalyzeFeedImportDecisionCommandHandler _handler;

    public AnalyzeFeedImportDecisionCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>()).Returns([]);
        _handler = new AnalyzeFeedImportDecisionCommandHandler(
            _repository, _pageFetcher,
            [new PlaneteBdLinkExtractor(NullLogger<PlaneteBdLinkExtractor>.Instance), new ZoneEbookLinkExtractor(NullLogger<ZoneEbookLinkExtractor>.Instance), new GenericDownloadLinkExtractor()],
            _bookReadService, _unitOfWork, Options.Create(_settings), NullLogger<AnalyzeFeedImportDecisionCommandHandler>.Instance);
    }

    private FeedImportDecision GivenDecision(string title = "Blacksad - Tome 3 - Âme rouge", string url = "https://www.planete-bd.org/blacksad-3")
    {
        var decision = FeedImportDecision.Create(s_userId, 7, title, url, null).Value!;
        _repository.GetByIdAsync(decision.Id, Arg.Any<CancellationToken>()).Returns(decision);
        return decision;
    }

    private void GivenPage(string html) =>
        _pageFetcher.GetHtmlAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>()).Returns(Result<string>.Success(html));

    private Task<Result> HandleAsync(FeedImportDecision decision) =>
        _handler.Handle(new AnalyzeFeedImportDecisionCommand(decision.Id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenDecisionDoesNotExist()
    {
        var result = await _handler.Handle(new AnalyzeFeedImportDecisionCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_FailWithoutFetching_WhenSourceHostIsNotAllowed()
    {
        var decision = GivenDecision(url: "https://evil.example/page");

        var result = await HandleAsync(decision);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorStep.Should().Be(AnalyzeFeedImportDecisionCommandHandler.SourceStep);
        decision.ErrorMessage.Should().Contain("Source domain not allowed");
        await _pageFetcher.DidNotReceive().GetHtmlAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenPageCannotBeFetched()
    {
        var decision = GivenDecision();
        _pageFetcher.GetHtmlAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>()).Returns(Result<string>.Failure(FeedImportError.PageUnavailable));

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorStep.Should().Be(AnalyzeFeedImportDecisionCommandHandler.FetchStep);
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenPageHasNoAllowedLink()
    {
        var decision = GivenDecision();
        GivenPage("<a href=\"https://other.example/x\">x</a>");

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorStep.Should().Be(AnalyzeFeedImportDecisionCommandHandler.ExtractionStep);
    }

    [Fact]
    public async Task Handle_Should_RecordMirrors_WhenSingleBookAndNoDuplicate()
    {
        var decision = GivenDecision();
        GivenPage("<a href=\"https://1fichier.com/?a\">1fichier</a><a href=\"https://rapidgator.net/file/b\">Rapidgator</a>");

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.ParsedSerie.Should().Be("Blacksad");
        decision.ParsedVolume.Should().Be(3);
        decision.GetCandidates().Should().ContainSingle().Which.Mirrors.Should().HaveCount(2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipWithoutDownload_WhenBookIsAlreadyInALibrary()
    {
        var decision = GivenDecision();
        var existing = new BookIdentityDto(Guid.CreateVersion7(), "Blacksad", "Âme rouge", 3, null, "BD numériques");
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>()).Returns([existing]);
        GivenPage("<a href=\"https://1fichier.com/?a\">1fichier</a>");

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.MatchedBookId.Should().Be(existing.Id);
        decision.Reason.Should().Be("Duplicate: \"Blacksad T3\" in \"BD numériques\".");
    }

    [Fact]
    public async Task Handle_Should_RequestArbitration_WhenLinksAreAmbiguous()
    {
        var decision = GivenDecision();
        GivenPage("<a href=\"https://1fichier.com/?a\">1</a><a href=\"https://1fichier.com/?b\">2</a>");

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.AwaitingArbitration);
        decision.ArbitrationKind.Should().Be(FeedImportArbitrationKind.AmbiguousLinks);
        decision.GetCandidates().Should().HaveCount(2);
        await _bookReadService.DidNotReceive().ListIdentitiesByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateOneDecisionPerBook_WhenArticleContainsSeveralBooks()
    {
        var decision = GivenDecision(title: "Blacksad - Tomes 1 à 2");
        GivenPage("""
            <a href="https://1fichier.com/?a">Blacksad_T01.cbz</a>
            <a href="https://1fichier.com/?b">Blacksad_T02.cbz</a>
            """);
        var added = new List<FeedImportDecision>();
        _repository.When(r => r.Add(Arg.Any<FeedImportDecision>())).Do(c => added.Add(c.Arg<FeedImportDecision>()));

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.ParsedVolume.Should().Be(1);
        var sibling = added.Should().ContainSingle().Subject;
        sibling.ItemIndex.Should().Be(1);
        sibling.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        sibling.ParsedSerie.Should().Be("Blacksad");
        sibling.ParsedVolume.Should().Be(2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DoNothing_WhenDecisionIsNoLongerPending()
    {
        var decision = GivenDecision();
        decision.Fail("Source", "x");

        var result = await HandleAsync(decision);

        result.IsSuccess.Should().BeTrue();
        await _pageFetcher.DidNotReceive().GetHtmlAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "FeedImports", "Fixtures", name));

    [Fact]
    public async Task Handle_Should_FetchOverHttpsAndSplitBooks_WhenRealMultiTomeArticle()
    {
        var decision = GivenDecision(
            title: "Metronom' - Les 5 tomes (Re-Up)",
            url: "http://planete-bd.org/bd/149155-metronom-les-5-tomes-re-up.html");
        GivenPage(Fixture("planete-bd_149155-metronom-les-5-tomes.html"));
        var added = new List<FeedImportDecision>();
        _repository.When(r => r.Add(Arg.Any<FeedImportDecision>())).Do(c => added.Add(c.Arg<FeedImportDecision>()));

        await HandleAsync(decision);

        await _pageFetcher.Received(1).GetHtmlAsync(
            new Uri("https://planete-bd.org/bd/149155-metronom-les-5-tomes-re-up.html"), Arg.Any<CancellationToken>());
        added.Should().HaveCount(4);
        new[] { decision }.Concat(added).Should().AllSatisfy(d =>
        {
            d.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
            d.ParsedSerie.Should().Be("Metronom");
        });
        new[] { decision }.Concat(added).Select(d => d.ParsedVolume).Should().BeEquivalentTo([1, 3, 4, 5, 2]);
    }

    [Fact]
    public async Task Handle_Should_FailWithLinksEachFile_WhenOnlyOfferedOnUnsupportedHost()
    {
        _settings.AllowedDownloadHosts = ["dailyuploads.net", "trbt.cc"];
        var decision = GivenDecision(
            title: "Old boy - L'intégrale",
            url: "https://zone-ebook.com/bd-comics-mangas/425275-old-boy-lintgrale.html");
        GivenPage(Fixture("zone-ebook_425275-old-boy-lintegrale.html"));
        var added = new List<FeedImportDecision>();
        _repository.When(r => r.Add(Arg.Any<FeedImportDecision>())).Do(c => added.Add(c.Arg<FeedImportDecision>()));

        await HandleAsync(decision);

        var decisions = new[] { decision }.Concat(added).ToList();
        decisions.Select(d => (d.ItemIndex, d.Status, d.ParsedVolume)).Should().Equal(
            (0, FeedImportDecisionStatus.LinksExtracted, 1),
            (1, FeedImportDecisionStatus.LinksExtracted, 3),
            (2, FeedImportDecisionStatus.Failed, 2),
            (3, FeedImportDecisionStatus.Failed, 4));
        var unsupported = decisions[2];
        unsupported.ErrorStep.Should().Be(AnalyzeFeedImportDecisionCommandHandler.ExtractionStep);
        unsupported.ErrorMessage.Should().Be($"{FeedImportError.DownloadHostNotSupported.Description}: fileq.net.");
        unsupported.GetCandidates().Should().ContainSingle().Which.Mirrors.Should().ContainSingle()
            .Which.Url.Should().Be("https://fileq.net/mr1etz20e0sg.html");
    }

    [Fact]
    public async Task Handle_Should_NotReportFile_WhenAnAllowedHostAlsoServesIt()
    {
        _settings.AllowedDownloadHosts = ["trbt.cc"];
        var decision = GivenDecision(title: "El Borbah - Tome 1");
        GivenPage(Fixture("planete-bd_151223-el-borbah-tome-1.html"));

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        decision.GetCandidates().Should().ContainSingle().Which.Mirrors.Should().ContainSingle().Which.Host.Should().Be("trbt.cc");
        _repository.DidNotReceive().Add(Arg.Any<FeedImportDecision>());
    }

    [Fact]
    public async Task Handle_Should_DetectDuplicateByPageIsbn_WhenRealArticleStatesIt()
    {
        var decision = GivenDecision(
            title: "Avaler La Lune - Tome 03 - Le Refuge (2026)",
            url: "http://zone-ebook.com/bd-comics-mangas/424424-avaler-la-lune-tome-03-le-refuge-2026.html");
        GivenPage(Fixture("zone-ebook_424424-avaler-la-lune-tome-03.html"));
        var existing = new BookIdentityDto(Guid.CreateVersion7(), "Une autre série", "Le Refuge", 9, "978-2-203-21158-2", "BD");
        _bookReadService.ListIdentitiesByUserAsync(s_userId, Arg.Any<CancellationToken>()).Returns([existing]);

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.SkippedDuplicate);
        decision.MatchedBookId.Should().Be(existing.Id);
        decision.ParsedSerie.Should().Be("Avaler La Lune");
        decision.ParsedVolume.Should().Be(3);
        decision.GetCandidates().Should().ContainSingle().Which.Mirrors.Should().HaveCount(3);
    }
}
