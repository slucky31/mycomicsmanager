using Application.FeedImports.Refresh;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.ImportJobs;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class RefreshFeedImportStatusesCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly IFeedImportDecisionRepository _decisionRepository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IImportJobRepository _importJobRepository = Substitute.For<IImportJobRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RefreshFeedImportStatusesCommandHandler _handler;

    public RefreshFeedImportStatusesCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _handler = new RefreshFeedImportStatusesCommandHandler(_decisionRepository, _importJobRepository, _unitOfWork);
    }

    private (FeedImportDecision Decision, ImportJob Job) GivenDownloaded()
    {
        var job = ImportJob.Create("Blacksad T03.cbz", "/data/import/A/Blacksad T03.cbz", 1_000, Guid.CreateVersion7()).Value!;
        var decision = FeedImportDecision.Create(s_userId, 7, "Blacksad - Tome 3", "https://planete-bd.org/blacksad-3", null).Value!;
        decision.RecordLinks(
            [new DownloadCandidate("Blacksad T03", "Blacksad T03.cbz", null, [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")])],
            ParsedComicTitle.Empty, "1 lien", FeedImportDecidedBy.Auto);
        decision.StartDownload();
        decision.MarkDownloaded("https://1fichier.com/?a", job.Id, "À trier");
        _decisionRepository.GetByStatusAsync(s_userId, FeedImportDecisionStatus.Downloaded, Arg.Any<CancellationToken>()).Returns([decision]);
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        return (decision, job);
    }

    private static void Complete(ImportJob job, Guid bookId)
    {
        foreach (var status in new[] { ImportJobStatus.Extracting, ImportJobStatus.Converting, ImportJobStatus.SearchingMetadata, ImportJobStatus.UploadingCover, ImportJobStatus.BuildingArchive })
        {
            job.Advance(status);
        }
        job.Complete(bookId);
    }

    private Task<Result> HandleAsync() =>
        _handler.Handle(new RefreshFeedImportStatusesCommand(s_userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_MarkImported_WhenImportJobCompleted()
    {
        var (decision, job) = GivenDownloaded();
        var bookId = Guid.CreateVersion7();
        Complete(job, bookId);

        var result = await HandleAsync();

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Imported);
        decision.DigitalBookId.Should().Be(bookId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_FailDecisionWithImportError_WhenImportJobFailed()
    {
        var (decision, job) = GivenDownloaded();
        job.Fail("Extraction", "Archive corrompue");

        await HandleAsync();

        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
        decision.ErrorStep.Should().Be(RefreshFeedImportStatusesCommandHandler.ImportStep);
        decision.ErrorMessage.Should().Be("L'import a échoué (Extraction) : Archive corrompue");
    }

    [Fact]
    public async Task Handle_Should_FailDecision_WhenImportJobNoLongerExists()
    {
        var (decision, job) = GivenDownloaded();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns((ImportJob?)null);

        await HandleAsync();

        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
    }

    [Fact]
    public async Task Handle_Should_LeaveDecisionAndNotSave_WhenImportIsStillRunning()
    {
        var (decision, job) = GivenDownloaded();
        job.Advance(ImportJobStatus.Extracting);

        var result = await HandleAsync();

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Downloaded);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        var result = await _handler.Handle(new RefreshFeedImportStatusesCommand(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.BadRequest);
    }
}
