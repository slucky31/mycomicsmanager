using System.Text;
using Application.Abstractions.Messaging;
using Application.ImportJobs;
using Application.ImportJobs.Create;
using Application.ImportJobs.Delete;
using Application.ImportJobs.ForceFail;
using Application.ImportJobs.GetById;
using Application.ImportJobs.List;
using Application.Interfaces;
using Application.Libraries.GetById;
using AwesomeAssertions;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class ImportServiceTests : IDisposable
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IQueryHandler<GetLibraryQuery, Library> _getLibraryHandler;
    private readonly ICommandHandler<CreateImportJobCommand, ImportJob> _createJobHandler;
    private readonly IImportJobEnqueuer _enqueuer;
    private readonly ImportService _service;
    private readonly string _importDir;
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _libraryId = Guid.CreateVersion7();
    private readonly Library _library;

    public ImportServiceTests()
    {
        _importDir = Path.Combine(Path.GetTempPath(), "import-service-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_importDir);

        _currentUserService = Substitute.For<ICurrentUserService>();
        _getLibraryHandler = Substitute.For<IQueryHandler<GetLibraryQuery, Library>>();
        _createJobHandler = Substitute.For<ICommandHandler<CreateImportJobCommand, ImportJob>>();
        _enqueuer = Substitute.For<IImportJobEnqueuer>();

        _library = Library.Create("My Comics", "#5C6BC0", "Bookmark", LibraryBookType.Digital, _userId).Value!;

        _currentUserService.GetCurrentUserIdAsync(Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(_userId));

        _getLibraryHandler.Handle(Arg.Any<GetLibraryQuery>(), Arg.Any<CancellationToken>())
            .Returns(_library);

        var importJob = ImportJob.Create("comic.cbz", "/tmp/comic.cbz", 1024, _libraryId).Value!;
        _createJobHandler.Handle(Arg.Any<CreateImportJobCommand>(), Arg.Any<CancellationToken>())
            .Returns(importJob);

        _enqueuer.Enqueue(Arg.Any<Guid>()).Returns("hangfire-job-id");

        var handlers = new ImportJobHandlers(
            Substitute.For<IQueryHandler<ListImportJobsQuery, IReadOnlyList<ImportJob>>>(),
            Substitute.For<IQueryHandler<GetImportJobQuery, ImportJob>>(),
            _createJobHandler,
            Substitute.For<ICommandHandler<DeleteImportJobCommand>>(),
            Substitute.For<ICommandHandler<ForceFailImportJobCommand>>(),
            _getLibraryHandler);

        var settings = Options.Create(new ImportSettings
        {
            ImportDirectory = _importDir,
            SupportedExtensions = [".cbz", ".cbr", ".zip", ".rar", ".pdf"],
            MaxFileSizeMb = 500
        });

        _service = new ImportService(handlers, _enqueuer, _currentUserService, settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_importDir))
        {
            Directory.Delete(_importDir, recursive: true);
        }
    }

    private static IBrowserFile CreateBrowserFile(string name, string content = "fake-content")
    {
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns(name);
        file.OpenReadStream(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        return file;
    }

    [Fact]
    public async Task UploadAndCreateJobAsync_Should_WriteFileToLibrarySubdirectory()
    {
        var file = CreateBrowserFile("comic.cbz");

        await _service.UploadAndCreateJobAsync(file, _libraryId, TestContext.Current.CancellationToken);

        var expectedSubDir = Path.Combine(_importDir, _library.ImportDirectoryName);
        var files = Directory.GetFiles(expectedSubDir);
        files.Should().HaveCount(1);
        Path.GetFileName(files[0]).Should().EndWith("_comic.cbz");
    }

    [Fact]
    public async Task UploadAndCreateJobAsync_Should_ReturnError_WhenLibraryNotFound()
    {
        _getLibraryHandler.Handle(Arg.Any<GetLibraryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<Library>.Failure(new TError("libraries:not-found", "Library not found")));

        var file = CreateBrowserFile("comic.cbz");

        var result = await _service.UploadAndCreateJobAsync(file, _libraryId, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAndCreateJobAsync_Should_ReturnError_WhenExtensionUnsupported()
    {
        var file = CreateBrowserFile("document.txt");

        var result = await _service.UploadAndCreateJobAsync(file, _libraryId, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        await _getLibraryHandler.DidNotReceive().Handle(Arg.Any<GetLibraryQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAndCreateJobAsync_Should_CleanUpFile_WhenJobCreationFails()
    {
        _createJobHandler.Handle(Arg.Any<CreateImportJobCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImportJob>.Failure(ImportJobError.BadRequest));

        var file = CreateBrowserFile("comic.cbz");

        await _service.UploadAndCreateJobAsync(file, _libraryId, TestContext.Current.CancellationToken);

        var expectedSubDir = Path.Combine(_importDir, _library.ImportDirectoryName);
        Directory.GetFiles(expectedSubDir).Should().BeEmpty();
    }
}
