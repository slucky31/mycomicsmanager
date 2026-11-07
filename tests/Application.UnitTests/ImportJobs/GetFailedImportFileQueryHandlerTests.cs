using Application.ImportJobs;
using Application.ImportJobs.DownloadFile;
using Application.Interfaces;
using Domain.Errors;
using Domain.ImportJobs;
using Domain.Libraries;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class GetFailedImportFileQueryHandlerTests
{
    private readonly IImportJobRepository _importJobRepository = Substitute.For<IImportJobRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IImportDirectoryStorage _storage = Substitute.For<IImportDirectoryStorage>();
    private readonly GetFailedImportFileQueryHandler _handler;
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Library _library;
    private readonly ImportJob _job;
    private const string ErrorFile = "/import/lib/errors/comic_20261107.cbz";

    public GetFailedImportFileQueryHandlerTests()
    {
        _library = Library.Create("Test", "#FF0000", "book", LibraryBookType.Digital, _userId).Value!;
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);

        _job = ImportJob.Create("comic.cbz", "/import/lib/comic.cbz", 1024, _library.Id).Value!;
        _job.Fail("Extracting", "Corrupted");
        _job.RecordErrorFile(ErrorFile);
        _importJobRepository.GetByIdAsync(_job.Id, Arg.Any<CancellationToken>()).Returns(_job);

        _storage.LocateFailedFile(_library.ImportDirectoryName, _job.OriginalFilePath, ErrorFile).Returns(ErrorFile);

        _handler = new GetFailedImportFileQueryHandler(new FailedImportJobLoader(_importJobRepository, _libraryRepository), _storage);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheFileWithItsOriginalName_WhenTheJobFailed()
    {
        var result = await _handler.Handle(new GetFailedImportFileQuery(_job.Id, _userId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new FailedImportFile(ErrorFile, "comic.cbz"));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailedFileNotFound_WhenTheFileIsGone()
    {
        _storage.LocateFailedFile(_library.ImportDirectoryName, _job.OriginalFilePath, ErrorFile)
            .Returns(ImportDirectoryStorageError.SourceFileNotFound);

        var result = await _handler.Handle(new GetFailedImportFileQuery(_job.Id, _userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportJobError.FailedFileNotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheLoaderError_WhenTheJobIsNotFailed()
    {
        var pending = ImportJob.Create("other.cbz", "/import/other.cbz", 1024, _library.Id).Value!;
        _importJobRepository.GetByIdAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);

        var result = await _handler.Handle(new GetFailedImportFileQuery(pending.Id, _userId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ImportJobError.NotFailed);
        _storage.DidNotReceive().LocateFailedFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }
}
