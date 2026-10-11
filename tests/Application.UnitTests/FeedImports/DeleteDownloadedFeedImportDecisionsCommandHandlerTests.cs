using Application.FeedImports.Delete;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.ImportJobs;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class DeleteDownloadedFeedImportDecisionsCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly ParsedComicTitle s_parsed = new("Blacksad", "Âme rouge", 3);
    private static readonly DownloadCandidate s_candidate = new("Blacksad T03", "Blacksad T03.cbz", null,
        [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);

    private readonly IFeedImportDecisionRepository _repository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IImportJobRepository _importJobRepository = Substitute.For<IImportJobRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteDownloadedFeedImportDecisionsCommandHandler _handler;

    public DeleteDownloadedFeedImportDecisionsCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _handler = new DeleteDownloadedFeedImportDecisionsCommandHandler(_repository, _importJobRepository, _unitOfWork);
    }

    private static ImportJob CreateJob(Action<ImportJob>? arrange = null)
    {
        var job = ImportJob.Create("Blacksad T03.cbz", "/imports/Blacksad T03.cbz", 1_000, Guid.CreateVersion7()).Value!;
        arrange?.Invoke(job);
        return job;
    }

    private static void Completed(ImportJob job)
    {
        job.Advance(ImportJobStatus.Extracting);
        job.Advance(ImportJobStatus.Converting);
        job.Advance(ImportJobStatus.SearchingMetadata);
        job.Advance(ImportJobStatus.UploadingCover);
        job.Advance(ImportJobStatus.BuildingArchive);
        job.Complete(Guid.CreateVersion7());
    }

    private static FeedImportDecision Downloaded(long entryId, ImportJob job)
    {
        var decision = FeedImportDecision.Create(s_userId, entryId, "Blacksad - Tome 3", $"https://planete-bd.org/{entryId}", null).Value!;
        decision.RecordLinks([s_candidate], s_parsed, "1 lien", FeedImportDecidedBy.Auto);
        decision.StartDownload();
        decision.MarkDownloaded("https://1fichier.com/?a", job.Id, "À trier");
        return decision;
    }

    private void Given(IReadOnlyList<FeedImportDecision> decisions, params ImportJob[] existingJobs)
    {
        _repository.GetByStatusAsync(s_userId, FeedImportDecisionStatus.Downloaded, Arg.Any<CancellationToken>()).Returns(decisions);
        _importJobRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(existingJobs);
    }

    private Task<Result<DeleteDownloadedFeedImportDecisionsResult>> HandleAsync(Guid? userId = null) =>
        _handler.Handle(new DeleteDownloadedFeedImportDecisionsCommand(userId ?? s_userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserIdIsEmpty()
    {
        var result = await HandleAsync(Guid.Empty);

        result.Error.Should().Be(FeedImportError.BadRequest);
        await _repository.DidNotReceiveWithAnyArgs().GetByStatusAsync(default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnZero_WhenNoDecisionIsDownloaded()
    {
        Given([]);

        var result = await HandleAsync();

        result.Value.Should().Be(new DeleteDownloadedFeedImportDecisionsResult(0, 0));
        await _importJobRepository.DidNotReceiveWithAnyArgs().GetByIdsAsync(default!, TestContext.Current.CancellationToken);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_DeleteOnlySucceededOrRemovedImports_WhenSomeImportsAreRunningOrFailed()
    {
        var completedJob = CreateJob(Completed);
        var removedJob = CreateJob();
        var runningJob = CreateJob();
        var failedJob = CreateJob(j => j.Fail("Extraction", "Archive corrompue"));
        var completed = Downloaded(1, completedJob);
        var removed = Downloaded(2, removedJob);
        var running = Downloaded(3, runningJob);
        var failed = Downloaded(4, failedJob);
        Given([completed, removed, running, failed], completedJob, runningJob, failedJob);

        var result = await HandleAsync();

        result.Value.Should().Be(new DeleteDownloadedFeedImportDecisionsResult(2, 2));
        _repository.Received(1).Remove(completed);
        _repository.Received(1).Remove(removed);
        _repository.DidNotReceive().Remove(running);
        _repository.DidNotReceive().Remove(failed);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotSave_WhenEveryImportIsRunningOrFailed()
    {
        var runningJob = CreateJob();
        Given([Downloaded(1, runningJob)], runningJob);

        var result = await HandleAsync();

        result.Value.Should().Be(new DeleteDownloadedFeedImportDecisionsResult(0, 1));
        _repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenSaveFails()
    {
        var completedJob = CreateJob(Completed);
        Given([Downloaded(1, completedJob)], completedJob);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(FeedImportError.BadRequest));

        var result = await HandleAsync();

        result.Error.Should().Be(FeedImportError.BadRequest);
    }
}
