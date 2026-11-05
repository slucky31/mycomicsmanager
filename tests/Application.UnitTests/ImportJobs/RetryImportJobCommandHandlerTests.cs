using Application.ImportJobs;
using Application.ImportJobs.Retry;
using Application.Interfaces;
using Domain.Errors;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class RetryImportJobCommandHandlerTests
{
    private readonly IImportJobRepository _importJobRepository = Substitute.For<IImportJobRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IImportDirectoryStorage _storage = Substitute.For<IImportDirectoryStorage>();
    private readonly IImportJobEnqueuer _enqueuer = Substitute.For<IImportJobEnqueuer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RetryImportJobCommandHandler _handler;
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Library _library;
    private readonly string _libraryDir;
    private readonly string _errorFile;
    private readonly ImportJob _job;

    public RetryImportJobCommandHandlerTests()
    {
        _library = Library.Create("Test", "#FF0000", "book", LibraryBookType.Digital, _userId).Value!;
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);
        _libraryDir = Path.Combine("/import", _library.ImportDirectoryName);
        _errorFile = Path.Combine(_libraryDir, "errors", "comic_20261104.cbz");

        _job = ImportJob.Create("comic.cbz", Path.Combine(_libraryDir, "comic.cbz"), 1024, _library.Id).Value!;
        _job.Fail("Extracting", "Corrupted");
        _job.RecordErrorFile(_errorFile);
        _importJobRepository.GetByIdAsync(_job.Id, Arg.Any<CancellationToken>()).Returns(_job);

        _storage.LocateFailedFile(_library.ImportDirectoryName, _job.OriginalFilePath, _errorFile).Returns(_errorFile);
        _storage.GetAvailableFilePath(_library.ImportDirectoryName, "comic.cbz").Returns(_job.OriginalFilePath);
        _storage.DepositFile(Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Success());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));

        _handler = new RetryImportJobCommandHandler(
            new FailedImportJobLoader(_importJobRepository, _libraryRepository),
            _importJobRepository, _storage, _enqueuer, _unitOfWork,
            NullLogger<RetryImportJobCommandHandler>.Instance);
    }

    private Task<Result> RetryAsync() =>
        _handler.Handle(new RetryImportJobCommand(_job.Id, _userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_SaveThePendingJobThenPutTheFileBackAndEnqueue_WhenTheFileIsInErrors()
    {
        var expectedPath = _job.OriginalFilePath;

        var result = await RetryAsync();

        result.IsSuccess.Should().BeTrue();
        _job.Status.Should().Be(ImportJobStatus.Pending);
        _job.OriginalFilePath.Should().Be(expectedPath);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _storage.DepositFile(_errorFile, expectedPath);
            _enqueuer.Enqueue(_job.Id);
        });
    }

    [Fact]
    public async Task Handle_Should_NotMoveTheFile_WhenItIsStillInTheLibraryDirectory()
    {
        _storage.LocateFailedFile(_library.ImportDirectoryName, _job.OriginalFilePath, _errorFile).Returns(_job.OriginalFilePath);

        var result = await RetryAsync();

        result.IsSuccess.Should().BeTrue();
        _storage.DidNotReceive().DepositFile(Arg.Any<string>(), Arg.Any<string>());
        _storage.DidNotReceive().GetAvailableFilePath(Arg.Any<string>(), Arg.Any<string>());
        _enqueuer.Received(1).Enqueue(_job.Id);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheLoaderError_WhenTheJobIsNotFailed()
    {
        var pending = ImportJob.Create("other.cbz", "/import/other.cbz", 1024, _library.Id).Value!;
        _importJobRepository.GetByIdAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);

        var result = await _handler.Handle(new RetryImportJobCommand(pending.Id, _userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportJobError.NotFailed);
        _storage.DidNotReceive().LocateFailedFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailedFileNotFound_WhenTheFileIsGone()
    {
        _storage.LocateFailedFile(_library.ImportDirectoryName, _job.OriginalFilePath, _errorFile)
            .Returns(ImportDirectoryStorageError.SourceFileNotFound);

        var result = await RetryAsync();

        result.Error.Should().Be(ImportJobError.FailedFileNotFound);
        _job.Status.Should().Be(ImportJobStatus.Failed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheStorageError_WhenNoDestinationIsAvailable()
    {
        _storage.GetAvailableFilePath(_library.ImportDirectoryName, "comic.cbz").Returns(ImportDirectoryStorageError.InvalidFileName);

        var result = await RetryAsync();

        result.Error.Should().Be(ImportDirectoryStorageError.InvalidFileName);
        _job.Status.Should().Be(ImportJobStatus.Failed);
    }

    [Fact]
    public async Task Handle_Should_NotMoveTheFile_WhenThePendingJobIsNotSaved()
    {
        var error = new TError("DB", "Save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(error));

        var result = await RetryAsync();

        result.Error.Should().Be(error);
        _storage.DidNotReceive().DepositFile(Arg.Any<string>(), Arg.Any<string>());
        _enqueuer.DidNotReceive().Enqueue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_FailTheJobAgainOnItsErrorFile_WhenTheFileCannotBeMovedBack()
    {
        _storage.DepositFile(_errorFile, Arg.Any<string>()).Returns(ImportDirectoryStorageError.DepositFailed);

        var result = await RetryAsync();

        result.Error.Should().Be(ImportDirectoryStorageError.DepositFailed);
        _job.Status.Should().Be(ImportJobStatus.Failed);
        _job.ErrorStep.Should().Be("Retry");
        _job.ErrorFilePath.Should().Be(_errorFile);
        _enqueuer.DidNotReceive().Enqueue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheMoveError_WhenTheFailureCannotBeSavedEither()
    {
        _storage.DepositFile(_errorFile, Arg.Any<string>()).Returns(ImportDirectoryStorageError.DepositFailed);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(1), Result<int>.Failure(new TError("DB", "Save failed")));

        var result = await RetryAsync();

        result.Error.Should().Be(ImportDirectoryStorageError.DepositFailed);
    }
}
