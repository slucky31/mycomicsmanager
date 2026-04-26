using Application.ImportJobs;
using Application.ImportJobs.Process;
using Application.Interfaces;
using Application.Libraries;
using Domain.Books;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Application.UnitTests.ImportJobs;

public class ProcessImportJobCommandHandlerTests
{
    private readonly ProcessImportJobCommandHandler _handler;
    private readonly IImportJobRepository _importJobRepository;
    private readonly IRepository<Library, Guid> _libraryRepository;
    private readonly IArchiveExtractor _archiveExtractor;
    private readonly IPdfImageExtractor _pdfImageExtractor;
    private readonly IImageProcessor _imageProcessor;
    private readonly IComicArchiveBuilder _archiveBuilder;
    private readonly IComicInfoXmlService _comicInfoXmlService;
    private readonly IComicSearchService _comicSearchService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILibraryLocalStorage _libraryLocalStorage;
    private readonly IImportDirectoryStorage _importDirectoryStorage;

    private static readonly Guid s_userId = Guid.CreateVersion7();
    private static readonly TError s_processingError = new("FP500", "Processing failed");

    public ProcessImportJobCommandHandlerTests()
    {
        _importJobRepository = Substitute.For<IImportJobRepository>();
        _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
        _archiveExtractor = Substitute.For<IArchiveExtractor>();
        _pdfImageExtractor = Substitute.For<IPdfImageExtractor>();
        _imageProcessor = Substitute.For<IImageProcessor>();
        _archiveBuilder = Substitute.For<IComicArchiveBuilder>();
        _comicInfoXmlService = Substitute.For<IComicInfoXmlService>();
        _comicSearchService = Substitute.For<IComicSearchService>();
        _cloudinaryService = Substitute.For<ICloudinaryService>();
        _bookRepository = Substitute.For<IBookRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _libraryLocalStorage = Substitute.For<ILibraryLocalStorage>();
        _importDirectoryStorage = Substitute.For<IImportDirectoryStorage>();

        _importDirectoryStorage.DeleteOriginalFile(Arg.Any<string>()).Returns(Result.Success());
        _importDirectoryStorage.MoveOriginalFileToError(Arg.Any<string>()).Returns(Result.Success());

        _handler = new ProcessImportJobCommandHandler(
            _importJobRepository, _libraryRepository,
            _archiveExtractor, _pdfImageExtractor,
            _imageProcessor, _archiveBuilder,
            _comicInfoXmlService, _comicSearchService,
            _cloudinaryService, _bookRepository,
            _unitOfWork, _libraryLocalStorage,
            _importDirectoryStorage,
            Options.Create(new ImportSettings { TempDirectory = Path.GetTempPath() }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ImportJob CreatePendingJob(string fileName = "comic.cbz", string filePath = "/srv/comic.cbz")
    {
        var libraryId = Guid.CreateVersion7();
        return ImportJob.Create(fileName, filePath, 10_240, libraryId).Value!;
    }

    private Library CreateDigitalLibrary(Guid libraryId)
    {
        var lib = Library.Create("Digital", "#000", "Icon", LibraryBookType.Digital, s_userId).Value!;
        _libraryRepository.GetByIdAsync(libraryId).Returns(lib);
        _libraryLocalStorage.rootPath.Returns(Path.GetTempPath());
        return lib;
    }

    private void SetupArchiveExtractor(ImportJob job, string[] imageFiles, string? comicInfoPath = null)
    {
        _pdfImageExtractor.CanHandle(job.OriginalFilePath).Returns(false);
        _archiveExtractor.CanHandle(job.OriginalFilePath).Returns(true);
        _archiveExtractor.ExtractAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ArchiveExtractionResult>.Success(
                new ArchiveExtractionResult(imageFiles, comicInfoPath)));
    }

    private void SetupPdfExtractor(ImportJob job, string[] imageFiles)
    {
        _pdfImageExtractor.CanHandle(job.OriginalFilePath).Returns(true);
        _pdfImageExtractor.ExtractImagesAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<PdfExtractionResult>.Success(
                new PdfExtractionResult(imageFiles, imageFiles.Length)));
    }

    private void SetupImageProcessor(int processedCount = 3, int skippedCount = 0, bool allAlreadyWebp = false)
    {
        _imageProcessor.ProcessImagesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<Func<ImageConversionProgress, Task>?>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                // Create the converted directory and a dummy cover so downstream steps can proceed
                var destDir = info.ArgAt<string>(1);
                Directory.CreateDirectory(destDir);
                File.WriteAllBytes(Path.Combine(destDir, "page-001.webp"), []);
                return Task.FromResult(Result<ImageProcessingResult>.Success(
                    new ImageProcessingResult(processedCount, skippedCount, allAlreadyWebp)));
            });
    }

    private void SetupArchiveBuilder(long fileSize = 5120, int pageCount = 3)
    {
        _archiveBuilder.BuildAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                // Create the output file so File.Move in the handler succeeds
                var destPath = info.ArgAt<string>(1);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                File.WriteAllBytes(destPath, []);
                return Task.FromResult(Result<ComicArchiveResult>.Success(
                    new ComicArchiveResult(destPath, fileSize, pageCount)));
            });
    }

    private void SetupCloudinary()
    {
        _cloudinaryService.UploadImageFromFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(new Uri("https://cdn.example.com/cover.webp"), "cover", true, null));
    }

    private void SetupComicInfoXml()
    {
        _comicInfoXmlService.Write(Arg.Any<string>(), Arg.Any<ComicInfoData>())
            .Returns(Result.Success());
    }

    private void SetupNoMetadataSearch()
    {
        _comicSearchService.SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ComicSearchResult("", "", "", 0, "", "", "", null, null, false));
    }

    // ── Job not found / wrong status ──────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnError_WhenJobNotFound()
    {
        _importJobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ImportJob?)null);

        var result = await _handler.Handle(new ProcessImportJobCommand(Guid.CreateVersion7()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenJobNotPending()
    {
        var job = CreatePendingJob();
        job.Advance(ImportJobStatus.Extracting); // already advanced
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.InvalidStatusTransition);
    }

    // ── Extraction ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ExtractArchive_WhenFileIsCbz()
    {
        var job = CreatePendingJob("comic.cbz", "/srv/comic.cbz");
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor(0, 0);
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        await _archiveExtractor.Received(1)
            .ExtractAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ExtractPdf_WhenFileIsPdf()
    {
        var job = CreatePendingJob("comic.pdf", "/srv/comic.pdf");
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupPdfExtractor(job, []);
        SetupImageProcessor(0, 0);
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        await _pdfImageExtractor.Received(1)
            .ExtractImagesAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Conversion ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ConvertImagesToWebp()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor(3, 0);
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        await _imageProcessor.Received(1)
            .ProcessImagesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<Func<ImageConversionProgress, Task>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipConversion_WhenAllAlreadyWebp()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor(0, 3, allAlreadyWebp: true);
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        _ = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        // Still calls ProcessImagesAsync (service decides internally); handler should still succeed
        await _imageProcessor.Received(1)
            .ProcessImagesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<Func<ImageConversionProgress, Task>?>(), Arg.Any<CancellationToken>());
    }

    // ── Metadata search───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_SearchMetadata_WhenIsbnFound()
    {
        // Filename contains a valid ISBN-13
        var job = CreatePendingJob("Serie 9782075162869.cbz", "/srv/Serie 9782075162869.cbz");
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();
        _comicSearchService.SearchByIsbnAsync("9782075162869", Arg.Any<CancellationToken>())
            .Returns(new ComicSearchResult("Title", "Serie", "9782075162869", 1, "", "Author", "Publisher", null, null, true));

        await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        await _comicSearchService.Received(1)
            .SearchByIsbnAsync("9782075162869", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipMetadataSearch_WhenNoIsbnFound()
    {
        var job = CreatePendingJob("My Comic Without Isbn.cbz", "/srv/noisbn.cbz");
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();

        await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        await _comicSearchService.DidNotReceive()
            .SearchByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Cover upload ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_UploadCoverToCloudinary()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        await _cloudinaryService.Received(1)
            .UploadImageFromFileAsync(Arg.Any<string>(), "digital-covers", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Archive build ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_BuildCbzArchive()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        await _archiveBuilder.Received(1)
            .BuildAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── DigitalBook creation ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_CreateDigitalBook()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder(fileSize: 4096);
        SetupNoMetadataSearch();

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<DigitalBook>();
        _bookRepository.Received(1).Add(Arg.Any<DigitalBook>());
    }

    // ── Full pipeline success ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenFullPipelineSucceeds()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder(fileSize: 8192);
        SetupNoMetadataSearch();

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    // ── Failure paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_FailJob_WhenExtractionFails()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);

        _pdfImageExtractor.CanHandle(job.OriginalFilePath).Returns(false);
        _archiveExtractor.ExtractAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ArchiveExtractionResult>.Failure(s_processingError));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        result.IsFailure.Should().BeTrue();
        _importJobRepository.Received().Update(Arg.Is<ImportJob>(j => j.Status == ImportJobStatus.Failed));
    }

    [Fact]
    public async Task Handle_Should_FailJob_WhenConversionFails()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);

        _imageProcessor.ProcessImagesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<Func<ImageConversionProgress, Task>?>(), Arg.Any<CancellationToken>())
            .Returns(Result<ImageProcessingResult>.Failure(s_processingError));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        result.IsFailure.Should().BeTrue();
        _importJobRepository.Received().Update(Arg.Is<ImportJob>(j => j.Status == ImportJobStatus.Failed));
    }

    [Fact]
    public async Task Handle_Should_FailJob_WhenArchiveBuildFails()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupCloudinary();
        SetupNoMetadataSearch();

        _archiveBuilder.BuildAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ComicArchiveResult>.Failure(s_processingError));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

        result.IsFailure.Should().BeTrue();
        _importJobRepository.Received().Update(Arg.Is<ImportJob>(j => j.Status == ImportJobStatus.Failed));
    }

    // ── Original file management ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_DeleteOriginalFile_WhenImportSucceeds()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var job = CreatePendingJob("comic.cbz", tempFile);
            _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
            CreateDigitalLibrary(job.LibraryId);
            SetupArchiveExtractor(job, []);
            SetupImageProcessor();
            SetupComicInfoXml();
            SetupCloudinary();
            SetupArchiveBuilder();
            SetupNoMetadataSearch();

            var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

            result.IsSuccess.Should().BeTrue();
            _importDirectoryStorage.Received(1).DeleteOriginalFile(tempFile);
            _importDirectoryStorage.DidNotReceive().MoveOriginalFileToError(Arg.Any<string>());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Handle_Should_MoveOriginalFileToError_WhenImportFails()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var job = CreatePendingJob("comic.cbz", tempFile);
            _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
            CreateDigitalLibrary(job.LibraryId);

            _pdfImageExtractor.CanHandle(job.OriginalFilePath).Returns(false);
            _archiveExtractor.ExtractAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Result<ArchiveExtractionResult>.Failure(s_processingError));

            var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), default);

            result.IsFailure.Should().BeTrue();
            _importDirectoryStorage.Received(1).MoveOriginalFileToError(tempFile);
            _importDirectoryStorage.DidNotReceive().DeleteOriginalFile(Arg.Any<string>());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
