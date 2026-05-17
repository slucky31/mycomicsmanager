using Application.ImportJobs;
using Application.ImportJobs.Process;
using Application.Interfaces;
using Domain.Books;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
    private readonly ITempWorkspace _tempWorkspace;
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
        _tempWorkspace = Substitute.For<ITempWorkspace>();
        _importDirectoryStorage = Substitute.For<IImportDirectoryStorage>();

        _importDirectoryStorage.DeleteOriginalFile(Arg.Any<string>()).Returns(Result.Success());
        _importDirectoryStorage.MoveOriginalFileToError(Arg.Any<string>()).Returns(Result.Success());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(0));

        _tempWorkspace.HasFreeSpace(Arg.Any<long>()).Returns(true);
        _tempWorkspace.CreateScratch(Arg.Any<Guid>()).Returns(callInfo =>
        {
            var tempDir = Path.Combine(Path.GetTempPath(), callInfo.Arg<Guid>().ToString());
            return (TempDir: tempDir,
                    RawDir: Path.Combine(tempDir, "raw"),
                    ConvertedDir: Path.Combine(tempDir, "converted"));
        });
        _tempWorkspace.GetWebpFiles(Arg.Any<string>()).Returns(["cover.webp"]);
        _tempWorkspace.MoveToLibrary(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => Result<string>.Success(
                Path.Combine(Path.GetTempPath(), "library", callInfo.ArgAt<string>(2))));

        _handler = new ProcessImportJobCommandHandler(
            new ProcessImportJobRepositories(
                _importJobRepository, _libraryRepository, _bookRepository, _unitOfWork),
            new ProcessImportJobFileProcessors(
                _archiveExtractor, _pdfImageExtractor, _imageProcessor, _archiveBuilder, _comicInfoXmlService),
            new ProcessImportJobExternalServices(
                _comicSearchService, _cloudinaryService, _tempWorkspace, _importDirectoryStorage));
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
            .Returns(Task.FromResult(Result<ImageProcessingResult>.Success(
                new ImageProcessingResult(processedCount, skippedCount, allAlreadyWebp))));
    }

    private void SetupArchiveBuilder(long fileSize = 5120, int pageCount = 3)
    {
        _archiveBuilder.BuildAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                var destPath = info.ArgAt<string>(1);
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

        var result = await _handler.Handle(new ProcessImportJobCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnError_WhenJobNotPending()
    {
        var job = CreatePendingJob();
        job.Advance(ImportJobStatus.Extracting); // already advanced
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        _ = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

            var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

            var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

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

    // ── Input validation ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenImportJobIdIsEmpty()
    {
        var result = await _handler.Handle(new ProcessImportJobCommand(Guid.Empty), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.BadRequest);
    }

    // ── Status validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnInvalidStatusTransition_WhenJobIsAlreadyFailed()
    {
        var job = CreatePendingJob();
        job.Fail("previous-step", "previous error");
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ImportJobError.InvalidStatusTransition);
        _importJobRepository.DidNotReceive().Update(Arg.Is<ImportJob>(j => j.Status == ImportJobStatus.Failed));
    }

    // ── Library not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenLibraryDoesNotExist()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        _libraryRepository.GetByIdAsync(job.LibraryId).Returns((Library?)null);

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(LibrariesError.NotFound);
    }

    // ── Metadata write failure ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_FailJob_WhenMetadataWriteFails()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();

        _comicInfoXmlService.Write(Arg.Any<string>(), Arg.Any<ComicInfoData>())
            .Returns(Result.Failure(s_processingError));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        _importJobRepository.Received().Update(Arg.Is<ImportJob>(j => j.Status == ImportJobStatus.Failed));
    }

    // ── Cover upload failure (best-effort) ────────────────────────────────────

    [Fact]
    public async Task Handle_Should_ContinuePipeline_WhenCoverUploadFails()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        _cloudinaryService.UploadImageFromFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CloudinaryUploadResult(null, null, false, "upload error"));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ImageLink.Should().BeEmpty();
    }

    // ── Cover upload skipped when no webp files ───────────────────────────────

    [Fact]
    public async Task Handle_Should_ContinuePipeline_WhenNoWebpFilesInConvertedDir()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);
        SetupArchiveExtractor(job, []);
        SetupImageProcessor(0, 0, allAlreadyWebp: true);
        SetupComicInfoXml();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        _tempWorkspace.GetWebpFiles(Arg.Any<string>()).Returns([]);

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _cloudinaryService.DidNotReceive()
            .UploadImageFromFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Unexpected exception handling ─────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_FailJob_WhenUnexpectedExceptionOccurs()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);

        _pdfImageExtractor.CanHandle(job.OriginalFilePath).Returns(false);
        _archiveExtractor.ExtractAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Disk read error"));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        _importJobRepository.Received().Update(Arg.Is<ImportJob>(j => j.Status == ImportJobStatus.Failed));
    }

    // ── PDF extraction failure ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_Should_FailJob_WhenPdfExtractionFails()
    {
        var job = CreatePendingJob("comic.pdf", "/srv/comic.pdf");
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);

        _pdfImageExtractor.CanHandle(job.OriginalFilePath).Returns(true);
        _pdfImageExtractor.ExtractImagesAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<PdfExtractionResult>.Failure(s_processingError));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        _importJobRepository.Received().Update(Arg.Is<ImportJob>(j => j.Status == ImportJobStatus.Failed));
    }

    // ── ComicInfo.xml read failure → pipeline continues ───────────────────────

    [Fact]
    public async Task Handle_Should_ContinuePipeline_WhenComicInfoReadFails()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);

        SetupArchiveExtractor(job, [], comicInfoPath: "/tmp/ComicInfo.xml");
        _comicInfoXmlService.Read(Arg.Any<string>()).Returns(Result<ComicInfoData>.Failure(s_processingError));
        SetupImageProcessor();
        SetupComicInfoXml();
        SetupCloudinary();
        SetupArchiveBuilder();
        SetupNoMetadataSearch();

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    // ── HandleOriginalFile failure branches (log-only) ────────────────────────

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenDeleteOriginalFileFails()
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

        _importDirectoryStorage.DeleteOriginalFile(Arg.Any<string>())
            .Returns(Result.Failure(s_processingError));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenMoveOriginalFileToErrorFails()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);

        _pdfImageExtractor.CanHandle(job.OriginalFilePath).Returns(false);
        _archiveExtractor.ExtractAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ArchiveExtractionResult>.Failure(s_processingError));

        _importDirectoryStorage.MoveOriginalFileToError(Arg.Any<string>())
            .Returns(Result.Failure(s_processingError));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    // ── FailJobAsync SaveChangesAsync failure (log-only) ─────────────────────

    [Fact]
    public async Task Handle_Should_ReturnError_WhenSaveFailsDuringJobFailure()
    {
        var job = CreatePendingJob();
        _importJobRepository.GetByIdAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        CreateDigitalLibrary(job.LibraryId);

        _pdfImageExtractor.CanHandle(job.OriginalFilePath).Returns(false);
        _archiveExtractor.ExtractAsync(job.OriginalFilePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ArchiveExtractionResult>.Failure(s_processingError));

        // First call = AdvanceAndSaveAsync (Extracting), second call = FailJobAsync
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(0), Result<int>.Failure(new TError("DB500", "save failed")));

        var result = await _handler.Handle(new ProcessImportJobCommand(job.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(s_processingError);
    }
}
