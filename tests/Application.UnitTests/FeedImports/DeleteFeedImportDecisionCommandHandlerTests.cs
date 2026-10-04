using Application.FeedImports.Delete;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.ImportJobs;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.FeedImports;

public class DeleteFeedImportDecisionCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly ParsedComicTitle s_parsed = new("Blacksad", "Âme rouge", 3);
    private static readonly DownloadCandidate s_candidate = new("Blacksad T03", "Blacksad T03.cbz", null,
        [new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);

    private readonly IFeedImportDecisionRepository _repository = Substitute.For<IFeedImportDecisionRepository>();
    private readonly IImportJobRepository _importJobRepository = Substitute.For<IImportJobRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly DeleteFeedImportDecisionCommandHandler _handler;

    public DeleteFeedImportDecisionCommandHandlerTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _handler = new DeleteFeedImportDecisionCommandHandler(_repository, _importJobRepository, _unitOfWork, _timeProvider);
    }

    private FeedImportDecision Given(Action<FeedImportDecision>? arrange = null)
    {
        var decision = FeedImportDecision.Create(s_userId, 9, "Blacksad - Tome 3", "https://planete-bd.org/b3", null).Value!;
        arrange?.Invoke(decision);
        _repository.GetByIdAsync(decision.Id, Arg.Any<CancellationToken>()).Returns(decision);
        return decision;
    }

    private static void Downloading(FeedImportDecision d)
    {
        d.RecordLinks([s_candidate], s_parsed, "1 lien", FeedImportDecidedBy.Auto);
        d.StartDownload();
    }

    private ImportJob GivenDownloadedWith(FeedImportDecision decision, Action<ImportJob> arrangeJob)
    {
        var job = ImportJob.Create("Blacksad T03.cbz", "/imports/Blacksad T03.cbz", 1_000, Guid.CreateVersion7()).Value!;
        arrangeJob(job);
        decision.MarkDownloaded("https://1fichier.com/?a", job.Id, "À trier");
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        return job;
    }

    private Task<Result> HandleAsync(Guid decisionId, Guid? userId = null) =>
        _handler.Handle(new DeleteFeedImportDecisionCommand(decisionId, userId ?? s_userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenDecisionIdIsEmpty()
    {
        var result = await HandleAsync(Guid.Empty);

        result.Error.Should().Be(FeedImportError.BadRequest);
        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenDecisionBelongsToAnotherUser()
    {
        var decision = Given();

        var result = await HandleAsync(decision.Id, Guid.CreateVersion7());

        result.Error.Should().Be(FeedImportError.NotFound);
        _repository.DidNotReceiveWithAnyArgs().Remove(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_RemoveAndSave_WhenDecisionIsNotInProgress()
    {
        var decision = Given(d => d.Fail("Analyse", "Page introuvable"));

        var result = await HandleAsync(decision.Id);

        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Remove(decision);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnDeleteInProgress_WhenDownloadIsRunning()
    {
        var decision = Given(Downloading);

        var result = await HandleAsync(decision.Id);

        result.Error.Should().Be(FeedImportError.DeleteInProgress);
        _repository.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnDeleteInProgress_WhenImportJobIsRunning()
    {
        var decision = Given(Downloading);
        GivenDownloadedWith(decision, _ => { });

        var result = await HandleAsync(decision.Id);

        result.Error.Should().Be(FeedImportError.DeleteInProgress);
        _repository.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_RemoveDecision_WhenImportJobIsFinished()
    {
        var decision = Given(Downloading);
        GivenDownloadedWith(decision, job => job.Fail("Extraction", "Archive corrompue"));

        var result = await HandleAsync(decision.Id);

        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Remove(decision);
    }

    [Fact]
    public async Task Handle_Should_RemoveDecision_WhenImportJobWasDeleted()
    {
        var decision = Given(Downloading);
        decision.MarkDownloaded("https://1fichier.com/?a", Guid.CreateVersion7(), "À trier");

        var result = await HandleAsync(decision.Id);

        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Remove(decision);
    }

    [Fact]
    public async Task Handle_Should_ReturnSaveError_WhenSaveFails()
    {
        var decision = Given();
        var error = new TError("DB_UPDATE_ERROR", "boom");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(error));

        var result = await HandleAsync(decision.Id);

        result.Error.Should().Be(error);
    }
}
