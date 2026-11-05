using Application.ImportJobs;
using Application.ImportJobs.Abandon;
using Application.Interfaces;
using Domain.Errors;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class AbandonImportJobCommandHandlerTests
{
    private readonly IImportJobRepository _importJobRepository = Substitute.For<IImportJobRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IImportDirectoryStorage _storage = Substitute.For<IImportDirectoryStorage>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AbandonImportJobCommandHandler _handler;
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Library _library;
    private readonly ImportJob _job;
    private const string ErrorFile = "/import/lib/errors/comic.cbz";

    public AbandonImportJobCommandHandlerTests()
    {
        _library = Library.Create("Test", "#FF0000", "book", LibraryBookType.Digital, _userId).Value!;
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);

        _job = ImportJob.Create("comic.cbz", "/import/lib/comic.cbz", 1024, _library.Id).Value!;
        _job.Fail("Extracting", "Corrupted");
        _job.RecordErrorFile(ErrorFile);
        _importJobRepository.GetByIdAsync(_job.Id, Arg.Any<CancellationToken>()).Returns(_job);

        _storage.LocateFailedFile(_library.ImportDirectoryName, _job.OriginalFilePath, ErrorFile).Returns(ErrorFile);
        _storage.DeleteOriginalFile(ErrorFile).Returns(Result.Success());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));

        _handler = new AbandonImportJobCommandHandler(
            new FailedImportJobLoader(_importJobRepository, _libraryRepository), _importJobRepository, _storage, _unitOfWork);
    }

    private Task<Result> AbandonAsync() =>
        _handler.Handle(new AbandonImportJobCommand(_job.Id, _userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_DeleteTheFileThenTheJob_WhenTheJobFailed()
    {
        var result = await AbandonAsync();

        result.IsSuccess.Should().BeTrue();
        Received.InOrder(() =>
        {
            _storage.DeleteOriginalFile(ErrorFile);
            _importJobRepository.Remove(_job);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_DeleteTheJob_WhenItsFileIsAlreadyGone()
    {
        _storage.LocateFailedFile(_library.ImportDirectoryName, _job.OriginalFilePath, ErrorFile)
            .Returns(ImportDirectoryStorageError.SourceFileNotFound);

        var result = await AbandonAsync();

        result.IsSuccess.Should().BeTrue();
        _storage.DidNotReceive().DeleteOriginalFile(Arg.Any<string>());
        _importJobRepository.Received(1).Remove(_job);
    }

    [Fact]
    public async Task Handle_Should_KeepTheJob_WhenTheFileCannotBeDeleted()
    {
        _storage.DeleteOriginalFile(ErrorFile).Returns(ImportDirectoryStorageError.InvalidPath);

        var result = await AbandonAsync();

        result.Error.Should().Be(ImportDirectoryStorageError.InvalidPath);
        _importJobRepository.DidNotReceive().Remove(Arg.Any<ImportJob>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheLoaderError_WhenTheJobIsNotFailed()
    {
        var pending = ImportJob.Create("other.cbz", "/import/other.cbz", 1024, _library.Id).Value!;
        _importJobRepository.GetByIdAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);

        var result = await _handler.Handle(new AbandonImportJobCommand(pending.Id, _userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportJobError.NotFailed);
        _storage.DidNotReceive().DeleteOriginalFile(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheSaveError_WhenTheDeletionIsNotSaved()
    {
        var error = new TError("DB", "Save failed");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(error));

        var result = await AbandonAsync();

        result.Error.Should().Be(error);
    }
}
