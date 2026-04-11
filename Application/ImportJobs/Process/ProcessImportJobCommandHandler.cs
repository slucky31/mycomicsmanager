using Application.Abstractions.Messaging;
using Application.Helpers;
using Application.Interfaces;
using Application.Libraries;
using Domain.Books;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Application.ImportJobs.Process;

public sealed class ProcessImportJobCommandHandler(
    IImportJobRepository importJobRepository,
    IRepository<Library, Guid> libraryRepository,
    IArchiveExtractor archiveExtractor,
    IPdfImageExtractor pdfImageExtractor,
    IImageProcessor imageProcessor,
    IComicArchiveBuilder archiveBuilder,
    IComicInfoXmlService comicInfoXmlService,
    IComicSearchService comicSearchService,
    ICloudinaryService cloudinaryService,
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork,
    ILibraryLocalStorage libraryLocalStorage,
    IOptions<ImportSettings> importSettings)
    : ICommandHandler<ProcessImportJobCommand, DigitalBook>
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ProcessImportJobCommandHandler>();
    private readonly ImportSettings _settings = importSettings.Value;

    private record BookMetadata(
        string Serie,
        string Title,
        string? Isbn,
        int VolumeNumber,
        string Authors,
        string Publishers,
        DateOnly? PublishDate,
        int PageCount);

    public async Task<Result<DigitalBook>> Handle(ProcessImportJobCommand request, CancellationToken cancellationToken)
    {
        if (request.ImportJobId == Guid.Empty)
        {
            return ImportJobError.BadRequest;
        }

        var importJob = await importJobRepository.GetByIdAsync(request.ImportJobId, cancellationToken);
        if (importJob is null)
        {
            return ImportJobError.NotFound;
        }

        if (importJob.Status != ImportJobStatus.Pending)
        {
            return ImportJobError.InvalidStatusTransition;
        }

        var library = await libraryRepository.GetByIdAsync(importJob.LibraryId);
        if (library is null)
        {
            return LibrariesError.NotFound;
        }

        var tempDir = Path.Combine(_settings.TempDirectory, importJob.Id.ToString());
        var rawDir = Path.Combine(tempDir, "raw");
        var convertedDir = Path.Combine(tempDir, "converted");

        // Check available disk space: require at least 3× the original file size
        var requiredBytes = importJob.OriginalFileSize * 3;
        try
        {
            var driveRoot = Path.GetPathRoot(_settings.TempDirectory) ?? "/";
            var drive = new DriveInfo(driveRoot);
            if (drive.AvailableFreeSpace < requiredBytes)
            {
                return await FailJobAsync(importJob, "Init", ImportJobError.InsufficientDiskSpace, cancellationToken);
            }
        }
#pragma warning disable CA1031 // DriveInfo may not work on all mount types; proceed anyway
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not check disk space for {TempDir}, proceeding anyway", _settings.TempDirectory);
        }
#pragma warning restore CA1031

        try
        {
            var extractResult = await ExtractStepAsync(importJob, rawDir, cancellationToken);
            if (extractResult.IsFailure)
            { return extractResult.Error!; }

            var convertResult = await ConvertStepAsync(importJob, rawDir, convertedDir, cancellationToken);
            if (convertResult.IsFailure)
            { return convertResult.Error!; }

            var metaResult = await SearchMetadataStepAsync(
                importJob, extractResult.Value, convertResult.Value!, convertedDir, cancellationToken);
            if (metaResult.IsFailure)
            { return metaResult.Error!; }

            var imageLink = await UploadCoverStepAsync(
                importJob, metaResult.Value!.Isbn, convertedDir, cancellationToken);

            var archiveResult = await BuildArchiveStepAsync(
                importJob, library, metaResult.Value!.Isbn, convertedDir, tempDir, cancellationToken);
            if (archiveResult.IsFailure)
            { return archiveResult.Error!; }

            return await CompleteStepAsync(
                importJob, metaResult.Value!, archiveResult.Value.FinalPath,
                archiveResult.Value.FileSize, imageLink, cancellationToken);
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    // ── Step 2: Extracting ────────────────────────────────────────────────────

    private async Task<Result<ComicInfoData?>> ExtractStepAsync(
        ImportJob importJob, string rawDir, CancellationToken ct)
    {
        await AdvanceAndSaveAsync(importJob, ImportJobStatus.Extracting, ct);

        string? comicInfoXmlPath = null;

        if (pdfImageExtractor.CanHandle(importJob.OriginalFilePath))
        {
            var pdfResult = await pdfImageExtractor.ExtractImagesAsync(
                importJob.OriginalFilePath, rawDir, ct);
            if (pdfResult.IsFailure)
            {
                return await FailJobAsync(importJob, "Extracting", pdfResult.Error!, ct);
            }
        }
        else
        {
            var archiveResult = await archiveExtractor.ExtractAsync(
                importJob.OriginalFilePath, rawDir, ct);
            if (archiveResult.IsFailure)
            {
                return await FailJobAsync(importJob, "Extracting", archiveResult.Error!, ct);
            }
            comicInfoXmlPath = archiveResult.Value!.ComicInfoXmlPath;
        }

        ComicInfoData? comicInfo = null;
        if (comicInfoXmlPath is not null)
        {
            var readResult = comicInfoXmlService.Read(comicInfoXmlPath);
            if (readResult.IsSuccess)
            {
                comicInfo = readResult.Value;
            }
        }

        return Result<ComicInfoData?>.Success(comicInfo);
    }

    // ── Step 3: Converting ────────────────────────────────────────────────────

    private async Task<Result<ImageProcessingResult>> ConvertStepAsync(
        ImportJob importJob, string rawDir, string convertedDir, CancellationToken ct)
    {
        await AdvanceAndSaveAsync(importJob, ImportJobStatus.Converting, ct);

        var convertResult = await imageProcessor.ProcessImagesAsync(rawDir, convertedDir, ct: ct);
        if (convertResult.IsFailure)
        {
            return await FailJobAsync(importJob, "Converting", convertResult.Error!, ct);
        }

        return convertResult.Value!;
    }

    // ── Step 4: SearchingMetadata ─────────────────────────────────────────────

    private async Task<Result<BookMetadata>> SearchMetadataStepAsync(
        ImportJob importJob,
        ComicInfoData? comicInfo,
        ImageProcessingResult conversion,
        string convertedDir,
        CancellationToken ct)
    {
        await AdvanceAndSaveAsync(importJob, ImportJobStatus.SearchingMetadata, ct);

        var isbn = FileNameIsbnExtractor.ExtractIsbn(importJob.OriginalFileName) ?? comicInfo?.Isbn;
        var serie = comicInfo?.Series ?? Path.GetFileNameWithoutExtension(importJob.OriginalFileName);
        var title = comicInfo?.Title ?? serie;
        var authors = comicInfo?.Writer ?? string.Empty;
        var publishers = comicInfo?.Publisher ?? string.Empty;
        var volumeNumber = comicInfo?.Number ?? 1;
        DateOnly? publishDate = comicInfo?.Year is not null
            ? new DateOnly(comicInfo.Year.Value, comicInfo.Month ?? 1, comicInfo.Day ?? 1)
            : null;

        if (!string.IsNullOrWhiteSpace(isbn))
        {
            var searchResult = await comicSearchService.SearchByIsbnAsync(isbn, ct);
            if (searchResult.Found)
            {
                serie = searchResult.Serie;
                title = searchResult.Title;
                authors = searchResult.Authors;
                publishers = searchResult.Publishers;
                publishDate = searchResult.PublishDate;
                volumeNumber = searchResult.VolumeNumber;
            }
        }

        var pageCount = conversion.ProcessedCount + conversion.SkippedCount;
        var updatedComicInfo = new ComicInfoData(
            Title: title, Series: serie, Number: volumeNumber, Summary: null,
            Year: publishDate?.Year, Month: publishDate?.Month, Day: publishDate?.Day,
            Writer: authors, Penciller: null, Publisher: publishers,
            Isbn: isbn, PageCount: pageCount > 0 ? pageCount : null);

        var writeResult = comicInfoXmlService.Write(Path.Combine(convertedDir, "ComicInfo.xml"), updatedComicInfo);
        if (writeResult.IsFailure)
        {
            return await FailJobAsync(importJob, "SearchingMetadata", writeResult.Error!, ct);
        }

        return new BookMetadata(serie, title, isbn, volumeNumber, authors, publishers, publishDate, pageCount);
    }

    // ── Step 5: UploadingCover (best-effort, never fails the pipeline) ────────

    private async Task<string> UploadCoverStepAsync(
        ImportJob importJob, string? isbn, string convertedDir, CancellationToken ct)
    {
        await AdvanceAndSaveAsync(importJob, ImportJobStatus.UploadingCover, ct);

        var coverFiles = Directory.Exists(convertedDir)
            ? Directory.GetFiles(convertedDir, "*.webp")
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : [];

        if (coverFiles.Count == 0)
        {
            return string.Empty;
        }

        var publicId = string.IsNullOrWhiteSpace(isbn) ? $"digital-{importJob.Id}" : isbn;
        var uploadResult = await cloudinaryService.UploadImageFromFileAsync(
            coverFiles[0], "digital-covers", publicId, ct);

        if (uploadResult.Success && uploadResult.Url is not null)
        {
            return uploadResult.Url.ToString();
        }

        Log.Warning("Cover upload failed for job {JobId}: {Error}", importJob.Id, uploadResult.Error);
        return string.Empty;
    }

    // ── Step 6: BuildingArchive ───────────────────────────────────────────────

    private async Task<Result<(string FinalPath, long FileSize)>> BuildArchiveStepAsync(
        ImportJob importJob,
        Library library,
        string? isbn,
        string convertedDir,
        string tempDir,
        CancellationToken ct)
    {
        await AdvanceAndSaveAsync(importJob, ImportJobStatus.BuildingArchive, ct);

        var outputFileName = string.IsNullOrWhiteSpace(isbn) ? $"{importJob.Id}.cbz" : $"{isbn}.cbz";
        var outputPath = Path.Combine(tempDir, outputFileName);

        var buildResult = await archiveBuilder.BuildAsync(convertedDir, outputPath, ct);
        if (buildResult.IsFailure)
        {
            return await FailJobAsync(importJob, "BuildingArchive", buildResult.Error!, ct);
        }

        var libraryDir = Path.Combine(libraryLocalStorage.rootPath, library.RelativePath);
        Directory.CreateDirectory(libraryDir);
        var finalPath = Path.Combine(libraryDir, outputFileName);
        File.Move(outputPath, finalPath, overwrite: true);

        return (finalPath, buildResult.Value!.FileSize);
    }

    // ── Step 7: Completed ─────────────────────────────────────────────────────

    private async Task<Result<DigitalBook>> CompleteStepAsync(
        ImportJob importJob,
        BookMetadata meta,
        string finalPath,
        long fileSize,
        string imageLink,
        CancellationToken ct)
    {
        var normalizedIsbn = string.IsNullOrWhiteSpace(meta.Isbn)
            ? $"IMP-{importJob.Id:N}"[..BookConstants.MaxIsbnLength]
            : IsbnHelper.NormalizeIsbn(meta.Isbn);

        var bookResult = DigitalBook.Create(
            meta.Serie, meta.Title, normalizedIsbn, importJob.LibraryId,
            finalPath, fileSize, meta.VolumeNumber, imageLink,
            meta.Authors, meta.Publishers, meta.PublishDate,
            meta.PageCount > 0 ? meta.PageCount : null);

        if (bookResult.IsFailure)
        {
            return await FailJobAsync(importJob, "Completed", bookResult.Error!, ct);
        }

        var digitalBook = bookResult.Value!;
        bookRepository.Add(digitalBook);

        importJob.Complete(digitalBook.Id);
        importJobRepository.Update(importJob);
        await unitOfWork.SaveChangesAsync(ct);

        return digitalBook;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task AdvanceAndSaveAsync(ImportJob importJob, ImportJobStatus status, CancellationToken ct)
    {
        importJob.Advance(status);
        importJobRepository.Update(importJob);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<TError> FailJobAsync(ImportJob importJob, string step, TError error, CancellationToken ct)
    {
        importJob.Fail(step, error.Description ?? error.Code);
        importJobRepository.Update(importJob);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
#pragma warning disable CA1031 // Best-effort persist: don't hide the original step error
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist job failure for job {JobId}", importJob.Id);
        }
#pragma warning restore CA1031
        return error;
    }

    private static void CleanupTempDirectory(string tempDir)
    {
        if (!Directory.Exists(tempDir))
        { return; }
        try
        {
            Directory.Delete(tempDir, true);
        }
#pragma warning disable CA1031 // Best-effort cleanup: catch all to avoid masking the original pipeline error
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not delete temp directory {Dir}", tempDir);
        }
#pragma warning restore CA1031
    }
}
