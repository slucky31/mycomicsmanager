using Application.Abstractions.Messaging;
using Application.ImportJobs;
using Application.ImportJobs.Create;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class FileWatcherServiceTests : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IImportJobEnqueuer _enqueuer;
    private readonly IRepository<Library, Guid> _libraryRepository;
    private readonly ICommandHandler<CreateImportJobCommand, ImportJob> _createHandler;
    private readonly IImportDirectoryStorage _importDirectoryStorage;
    private readonly FileWatcherService _service;
    private readonly string _importDir;
    private readonly Guid _libraryId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();

    public FileWatcherServiceTests()
    {
        _importDir = Path.Combine(Path.GetTempPath(), "fws-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_importDir);

        _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
        _createHandler = Substitute.For<ICommandHandler<CreateImportJobCommand, ImportJob>>();
        _enqueuer = Substitute.For<IImportJobEnqueuer>();
        _importDirectoryStorage = Substitute.For<IImportDirectoryStorage>();

        // Wire up library repository to return a digital library
        var library = Library.Create("Comics", "#5C6BC0", "Bookmark", LibraryBookType.Digital, _userId).Value!;
        _libraryRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(library);
        _libraryRepository.ListAsync().Returns([library]);

        // Wire up create handler to return a successful import job
        var importJob = ImportJob.Create("comic.cbz", "/tmp/comic.cbz", 1024, _libraryId).Value!;
        _createHandler.Handle(Arg.Any<CreateImportJobCommand>(), Arg.Any<CancellationToken>())
            .Returns(importJob);

        _enqueuer.Enqueue(Arg.Any<Guid>()).Returns("hangfire-job-id");
        _importDirectoryStorage.EnsureExists(Arg.Any<string>()).Returns(Result.Success());

        // Build scope chain.
        // CreateAsyncScope() is an extension method that calls CreateScope() internally —
        // mock CreateScope() only; the real extension wraps it in AsyncServiceScope.
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IRepository<Library, Guid>)).Returns(_libraryRepository);
        serviceProvider.GetService(typeof(ICommandHandler<CreateImportJobCommand, ImportJob>)).Returns(_createHandler);
        serviceProvider.GetService(typeof(IImportDirectoryStorage)).Returns(_importDirectoryStorage);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(scope);

        var settings = Options.Create(new ImportSettings
        {
            ImportDirectory = _importDir,
            PollingIntervalSeconds = 3600, // prevent polling during tests
            SupportedExtensions = [".cbz", ".cbr", ".zip", ".rar", ".pdf"]
        });

        _service = new FileWatcherService(_scopeFactory, _enqueuer, settings);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_importDir))
        {
            Directory.Delete(_importDir, true);
        }
    }

    [Fact]
    public async Task StartAsync_Should_ScanExistingFiles()
    {
        var libDir = Path.Combine(_importDir, _libraryId.ToString());
        Directory.CreateDirectory(libDir);
        await File.WriteAllBytesAsync(Path.Combine(libDir, "existing.cbz"), new byte[100], TestContext.Current.CancellationToken);

        await _service.StartAsync(CancellationToken.None);

        await _createHandler.Received(1).Handle(
            Arg.Is<CreateImportJobCommand>(c => c.OriginalFileName == "existing.cbz"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_Should_InitializeWatcher()
    {
        // StartAsync should complete without error and the service should be running
        var act = async () => await _service.StartAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_Should_NotThrow()
    {
        await _service.StartAsync(CancellationToken.None);

        var act = async () => await _service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessFileAsync_Should_IgnoreUnsupportedExtensions()
    {
        var filePath = Path.Combine(_importDir, _libraryId.ToString(), "image.jpg");

        await _service.ProcessFileAsync(filePath, CancellationToken.None);

        await _createHandler.DidNotReceive().Handle(
            Arg.Any<CreateImportJobCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessFileAsync_Should_IgnoreFilesAtRoot_WhenNoLibrarySubfolder()
    {
        // File is directly in import root — parent name is not a GUID
        var filePath = Path.Combine(_importDir, "comic.cbz");

        await _service.ProcessFileAsync(filePath, CancellationToken.None);

        await _createHandler.DidNotReceive().Handle(
            Arg.Any<CreateImportJobCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessFileAsync_Should_CreateImportJob_WhenFileIsReady()
    {
        var libDir = Path.Combine(_importDir, _libraryId.ToString());
        Directory.CreateDirectory(libDir);
        var filePath = Path.Combine(libDir, "comic.cbz");
        await File.WriteAllBytesAsync(filePath, new byte[512], TestContext.Current.CancellationToken);

        await _service.ProcessFileAsync(filePath, CancellationToken.None);

        await _createHandler.Received(1).Handle(
            Arg.Is<CreateImportJobCommand>(c =>
                c.OriginalFileName == "comic.cbz" &&
                c.LibraryId == _libraryId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessFileAsync_Should_EnqueueHangfireJob_AfterCreatingImportJob()
    {
        var libDir = Path.Combine(_importDir, _libraryId.ToString());
        Directory.CreateDirectory(libDir);
        var filePath = Path.Combine(libDir, "comic.cbz");
        await File.WriteAllBytesAsync(filePath, new byte[512], TestContext.Current.CancellationToken);

        await _service.ProcessFileAsync(filePath, CancellationToken.None);

        _enqueuer.Received(1).Enqueue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ProcessFileAsync_Should_NotEnqueue_WhenCreateImportJobFails()
    {
        _createHandler.Handle(Arg.Any<CreateImportJobCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImportJob>.Failure(new TError("ERR", "Fail")));

        var libDir = Path.Combine(_importDir, _libraryId.ToString());
        Directory.CreateDirectory(libDir);
        var filePath = Path.Combine(libDir, "comic.cbz");
        await File.WriteAllBytesAsync(filePath, new byte[512], TestContext.Current.CancellationToken);

        await _service.ProcessFileAsync(filePath, CancellationToken.None);

        _enqueuer.DidNotReceive().Enqueue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ProcessFileAsync_Should_IgnoreFile_WhenLibraryNotFound()
    {
        _libraryRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Library?)null);

        var libDir = Path.Combine(_importDir, _libraryId.ToString());
        Directory.CreateDirectory(libDir);
        var filePath = Path.Combine(libDir, "comic.cbz");
        await File.WriteAllBytesAsync(filePath, new byte[512], TestContext.Current.CancellationToken);

        await _service.ProcessFileAsync(filePath, CancellationToken.None);

        await _createHandler.DidNotReceive().Handle(
            Arg.Any<CreateImportJobCommand>(),
            Arg.Any<CancellationToken>());
    }
}
