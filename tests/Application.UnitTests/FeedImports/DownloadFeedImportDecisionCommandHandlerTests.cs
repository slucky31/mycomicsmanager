using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.Download;
using Application.ImportJobs;
using Application.ImportJobs.Create;
using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Create;
using Domain.Errors;
using Domain.FeedImports;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class DownloadFeedImportDecisionCommandHandlerTests
{
    private const string FichierUrl = "https://1fichier.com/?abc";
    private const string RapidgatorUrl = "https://rapidgator.net/file/x/Blacksad T03.cbz.html";
    private const string TempPath = "/data/temp/feed-imports/file.download";
    private const string TargetPath = "/data/import/A TRIER/Blacksad T03.cbz";

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly Uri s_downloadUrl = new("https://srv1.debrid.link/dl/abc/Blacksad T03.cbz");

    private readonly IFeedImportDecisionRepository _repository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDebridLinkClient _debridLink = Substitute.For<IDebridLinkClient>();
    private readonly IFeedImportFileDownloader _downloader = Substitute.For<IFeedImportFileDownloader>();
    private readonly ILibraryReadService _libraryReadService = Substitute.For<ILibraryReadService>();
    private readonly ICommandHandler<CreateLibraryCommand, Library> _createLibrary = Substitute.For<ICommandHandler<CreateLibraryCommand, Library>>();
    private readonly IImportDirectoryStorage _storage = Substitute.For<IImportDirectoryStorage>();
    private readonly ICommandHandler<CreateImportJobCommand, ImportJob> _createImportJob = Substitute.For<ICommandHandler<CreateImportJobCommand, ImportJob>>();
    private readonly IImportJobEnqueuer _enqueuer = Substitute.For<IImportJobEnqueuer>();
    private readonly FeedImportSettings _feedImportSettings = new()
    {
        TargetLibraryName = "À trier",
        AllowedDownloadHosts = ["1fichier.com", "rapidgator.net"]
    };
    private readonly DebridLinkSettings _debridLinkSettings = new()
    {
        ApiKey = "key",
        HosterPriority = ["1fichier.com", "rapidgator.net"],
        DownloadHosts = ["debrid.link"]
    };
    private readonly ImportSettings _importSettings = new() { MaxFileSizeMb = 10 };
    private readonly Library _library = Library.Create("À trier", "#5C6BC0", "CollectionsBookmark", LibraryBookType.Digital, s_userId).Value!;
    private readonly ImportJob _importJob;
    private readonly DownloadFeedImportDecisionCommandHandler _handler;

    public DownloadFeedImportDecisionCommandHandlerTests()
    {
        _importJob = ImportJob.Create("Blacksad T03.cbz", TargetPath, 1_000, _library.Id).Value!;
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _debridLink.GetSupportedDomainsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<string>>.Success(["1fichier.com", "rapidgator.net"]));
        _debridLink.UnlockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<DebridLinkFile>.Success(new DebridLinkFile("Blacksad T03.cbz", 1_000, s_downloadUrl)));
        _downloader.DownloadAsync(Arg.Any<Uri>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<DownloadedFile>.Success(new DownloadedFile(TempPath, 1_000)));
        _libraryReadService.GetByNameAsync("À trier", s_userId, Arg.Any<CancellationToken>()).Returns(_library);
        _storage.EnsureExists(Arg.Any<string>()).Returns(Result.Success());
        _storage.GetAvailableFilePath(_library.ImportDirectoryName, "Blacksad T03.cbz").Returns(Result<string>.Success(TargetPath));
        _storage.DepositFile(TempPath, TargetPath).Returns(Result.Success());
        _createImportJob.Handle(Arg.Any<CreateImportJobCommand>(), Arg.Any<CancellationToken>()).Returns(_importJob);

        _handler = new DownloadFeedImportDecisionCommandHandler(
            _repository,
            _unitOfWork,
            new FeedImportDownloadServices(_debridLink, _downloader),
            new FeedImportDepositServices(_libraryReadService, _createLibrary, _storage, _createImportJob, _enqueuer),
            new FeedImportDownloadOptions(Options.Create(_feedImportSettings), Options.Create(_debridLinkSettings), Options.Create(_importSettings)));
    }

    private FeedImportDecision GivenDecision()
    {
        var decision = FeedImportDecision.Create(s_userId, 7, "Blacksad - Tome 3", "https://planete-bd.org/blacksad-3", null).Value!;
        var candidate = new DownloadCandidate("Blacksad T03", "Blacksad T03.cbz", 1_000,
            [new DownloadMirror(RapidgatorUrl, "rapidgator.net"), new DownloadMirror(FichierUrl, "1fichier.com")]);
        decision.RecordLinks([candidate], new ParsedComicTitle("Blacksad", null, 3), "2 miroirs", FeedImportDecidedBy.Auto);
        _repository.GetByIdAsync(decision.Id, Arg.Any<CancellationToken>()).Returns(decision);
        return decision;
    }

    private Task<Result> HandleAsync(FeedImportDecision decision) =>
        _handler.Handle(new DownloadFeedImportDecisionCommand(decision.Id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_DepositFileAndLinkImportJob_WhenPreferredMirrorWorks()
    {
        var decision = GivenDecision();

        var result = await HandleAsync(decision);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Downloaded);
        decision.ImportJobId.Should().Be(_importJob.Id);
        decision.ChosenMirror.Should().Be(FichierUrl);
        await _debridLink.Received(1).UnlockAsync(FichierUrl, Arg.Any<CancellationToken>());
        await _debridLink.DidNotReceive().UnlockAsync(RapidgatorUrl, Arg.Any<CancellationToken>());
        await _createImportJob.Received(1).Handle(
            new CreateImportJobCommand("Blacksad T03.cbz", TargetPath, 1_000, _library.Id, s_userId), Arg.Any<CancellationToken>());
        _storage.Received(1).DepositFile(TempPath, TargetPath);
        _enqueuer.Received(1).Enqueue(_importJob.Id);
    }

    [Fact]
    public async Task Handle_Should_CreateImportJobBeforeDepositingFile_WhenDownloadSucceeds()
    {
        var decision = GivenDecision();

        await HandleAsync(decision);

        Received.InOrder(() =>
        {
            _createImportJob.Handle(Arg.Any<CreateImportJobCommand>(), Arg.Any<CancellationToken>());
            _storage.DepositFile(TempPath, TargetPath);
            _enqueuer.Enqueue(_importJob.Id);
        });
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorizedWithoutLoading_WhenApiKeyIsMissing()
    {
        _debridLinkSettings.ApiKey = " ";
        var decision = GivenDecision();

        var result = await HandleAsync(decision);

        result.Error.Should().Be(FeedImportError.DebridLinkUnauthorized);
        decision.Status.Should().Be(FeedImportDecisionStatus.LinksExtracted);
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DoNothing_WhenDecisionIsNotLinksExtracted()
    {
        var decision = FeedImportDecision.Create(s_userId, 8, "Pending", "https://planete-bd.org/8", null).Value!;
        _repository.GetByIdAsync(decision.Id, Arg.Any<CancellationToken>()).Returns(decision);

        var result = await HandleAsync(decision);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Pending);
        await _debridLink.DidNotReceive().UnlockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateDigitalTargetLibrary_WhenItDoesNotExist()
    {
        _libraryReadService.GetByNameAsync("À trier", s_userId, Arg.Any<CancellationToken>()).Returns((Library?)null);
        _createLibrary.Handle(Arg.Any<CreateLibraryCommand>(), Arg.Any<CancellationToken>()).Returns(_library);
        var decision = GivenDecision();

        await HandleAsync(decision);

        await _createLibrary.Received(1).Handle(
            Arg.Is<CreateLibraryCommand>(c => c.Name == "À trier" && c.BookType == LibraryBookType.Digital && c.UserId == s_userId),
            Arg.Any<CancellationToken>());
        decision.Status.Should().Be(FeedImportDecisionStatus.Downloaded);
    }

    [Fact]
    public async Task Handle_Should_FailBeforeDownloading_WhenTargetLibraryIsPhysical()
    {
        var physical = Library.Create("À trier", "#5C6BC0", "CollectionsBookmark", LibraryBookType.Physical, s_userId).Value!;
        _libraryReadService.GetByNameAsync("À trier", s_userId, Arg.Any<CancellationToken>()).Returns(physical);
        var decision = GivenDecision();

        var result = await HandleAsync(decision);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorStep.Should().Be(DownloadFeedImportDecisionCommandHandler.LibraryStep);
        decision.ErrorMessage.Should().Be(FeedImportError.TargetLibraryInvalid.Description);
        await _debridLink.DidNotReceive().UnlockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_TryNextMirrorAndTraceFailure_WhenFirstMirrorIsUnavailable()
    {
        _debridLink.UnlockAsync(FichierUrl, Arg.Any<CancellationToken>())
            .Returns(Result<DebridLinkFile>.Failure(FeedImportError.DebridLinkFileUnavailable));
        var decision = GivenDecision();

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.Downloaded);
        decision.ChosenMirror.Should().Be(RapidgatorUrl);
        decision.Events.Should().Contain(e => e.Description.StartsWith("Échec du miroir 1fichier.com", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_Should_StopWithoutTryingOtherMirrors_WhenQuotaIsReached()
    {
        _debridLink.UnlockAsync(FichierUrl, Arg.Any<CancellationToken>())
            .Returns(Result<DebridLinkFile>.Failure(FeedImportError.DebridLinkQuotaReached));
        var decision = GivenDecision();

        var result = await HandleAsync(decision);

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorStep.Should().Be(DownloadFeedImportDecisionCommandHandler.DownloadStep);
        decision.ErrorMessage.Should().Be(FeedImportError.DebridLinkQuotaReached.Description);
        await _debridLink.DidNotReceive().UnlockAsync(RapidgatorUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipMirrorsDebridLinkDoesNotSupport_WhenDomainListIsAvailable()
    {
        _debridLink.GetSupportedDomainsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<string>>.Success(["rapidgator.net"]));
        var decision = GivenDecision();

        await HandleAsync(decision);

        await _debridLink.DidNotReceive().UnlockAsync(FichierUrl, Arg.Any<CancellationToken>());
        decision.ChosenMirror.Should().Be(RapidgatorUrl);
    }

    [Fact]
    public async Task Handle_Should_NotDownload_WhenDownloadUrlHostIsNotAllowed()
    {
        _debridLink.UnlockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<DebridLinkFile>.Success(new DebridLinkFile("Blacksad T03.cbz", 1_000, new Uri("https://evil.example/file.cbz"))));
        var decision = GivenDecision();

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorMessage.Should().Contain("evil.example");
        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<Uri>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_FailWithoutDownloading_WhenFileTypeIsNotSupported()
    {
        _debridLink.UnlockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<DebridLinkFile>.Success(new DebridLinkFile("Blacksad T03.exe", 1_000, s_downloadUrl)));
        var decision = GivenDecision();

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorMessage.Should().Contain("Blacksad T03.exe");
        await _debridLink.Received(1).UnlockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<Uri>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_FailWithoutDownloading_WhenAnnouncedSizeExceedsLimit()
    {
        _debridLink.UnlockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<DebridLinkFile>.Success(new DebridLinkFile("Blacksad T03.cbz", 11L * 1024 * 1024, s_downloadUrl)));
        var decision = GivenDecision();

        await HandleAsync(decision);

        decision.ErrorMessage.Should().Be(FeedImportError.FileTooLarge.Description);
        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<Uri>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DiscardTempFileWithoutDeposit_WhenImportJobCannotBeCreated()
    {
        _createImportJob.Handle(Arg.Any<CreateImportJobCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImportJob>.Failure(ImportJobError.AlreadyQueued));
        var decision = GivenDecision();

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorStep.Should().Be(DownloadFeedImportDecisionCommandHandler.ImportStep);
        _downloader.Received(1).Discard(TempPath);
        _storage.DidNotReceiveWithAnyArgs().DepositFile(default!, default!);
        _enqueuer.DidNotReceiveWithAnyArgs().Enqueue(default);
    }

    [Fact]
    public async Task Handle_Should_FailImportJobAndDecision_WhenDepositFails()
    {
        _storage.DepositFile(TempPath, TargetPath).Returns(Result.Failure(ImportDirectoryStorageError.DepositFailed));
        var decision = GivenDecision();

        await HandleAsync(decision);

        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        _importJob.Status.Should().Be(ImportJobStatus.Failed);
        _downloader.Received(1).Discard(TempPath);
        _enqueuer.DidNotReceiveWithAnyArgs().Enqueue(default);
    }

    [Fact]
    public void OrderByPriority_Should_SortByConfiguredHostsAndKeepPageOrderForOthers()
    {
        IReadOnlyList<DownloadMirror> mirrors =
        [
            new("https://katfile.biz/a", "katfile.biz"),
            new("https://fileq.net/b", "fileq.net"),
            new("https://www.rapidgator.net/c", "www.rapidgator.net"),
            new("https://1fichier.com/d", "1fichier.com")
        ];

        var ordered = DownloadFeedImportDecisionCommandHandler.OrderByPriority(mirrors, ["1fichier.com", "rapidgator.net"]);

        ordered.Select(m => m.Host).Should().Equal("1fichier.com", "www.rapidgator.net", "katfile.biz", "fileq.net");
    }
}
