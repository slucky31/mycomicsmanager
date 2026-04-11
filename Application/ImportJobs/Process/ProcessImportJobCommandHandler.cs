using Application.Abstractions.Messaging;
using Application.Helpers;
using Application.Interfaces;
using Application.Libraries;
using Domain.Books;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;

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
    ILibraryLocalStorage libraryLocalStorage)
    : ICommandHandler<ProcessImportJobCommand, DigitalBook>
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ProcessImportJobCommandHandler>();

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

        var tempDir = Path.Combine(Path.GetTempPath(), "mycomicsmanager", importJob.Id.ToString());
        var rawDir = Path.Combine(tempDir, "raw");
        var convertedDir = Path.Combine(tempDir, "converted");

        try
        {
            // ── Step 2: Extracting ────────────────────────────────────────────
            importJob.Advance(ImportJobStatus.Extracting);
            importJobRepository.Update(importJob);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            string? comicInfoXmlPath = null;

            if (pdfImageExtractor.CanHandle(importJob.OriginalFilePath))
            {
                var pdfResult = await pdfImageExtractor.ExtractImagesAsync(
                    importJob.OriginalFilePath, rawDir, cancellationToken);
                if (pdfResult.IsFailure)
                {
                    return await FailJobAsync(importJob, "Extracting", pdfResult.Error!, cancellationToken);
                }
            }
            else
            {
                var archiveResult = await archiveExtractor.ExtractAsync(
                    importJob.OriginalFilePath, rawDir, cancellationToken);
                if (archiveResult.IsFailure)
                {
                    return await FailJobAsync(importJob, "Extracting", archiveResult.Error!, cancellationToken);
                }
                comicInfoXmlPath = archiveResult.Value!.ComicInfoXmlPath;
            }

            // Read ComicInfo.xml if present
            ComicInfoData? comicInfo = null;
            if (comicInfoXmlPath is not null)
            {
                var readResult = comicInfoXmlService.Read(comicInfoXmlPath);
                if (readResult.IsSuccess)
                {
                    comicInfo = readResult.Value;
                }
            }

            // ── Step 3: Converting ────────────────────────────────────────────
            importJob.Advance(ImportJobStatus.Converting);
            importJobRepository.Update(importJob);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var convertResult = await imageProcessor.ProcessImagesAsync(rawDir, convertedDir, ct: cancellationToken);
            if (convertResult.IsFailure)
            {
                return await FailJobAsync(importJob, "Converting", convertResult.Error!, cancellationToken);
            }

            // ── Step 4: SearchingMetadata ─────────────────────────────────────
            importJob.Advance(ImportJobStatus.SearchingMetadata);
            importJobRepository.Update(importJob);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var isbn = FileNameIsbnExtractor.ExtractIsbn(importJob.OriginalFileName)
                       ?? comicInfo?.Isbn;

            var serie = comicInfo?.Series ?? Path.GetFileNameWithoutExtension(importJob.OriginalFileName);
            var title = comicInfo?.Title ?? serie;
            var authors = comicInfo?.Writer ?? string.Empty;
            var publishers = comicInfo?.Publisher ?? string.Empty;
            var volumeNumber = comicInfo?.Number ?? 1;
            DateOnly? publishDate = comicInfo?.Year is not null
                ? new DateOnly(comicInfo.Year.Value, comicInfo.Month ?? 1, comicInfo.Day ?? 1)
                : null;
            var imageLink = string.Empty;

            if (!string.IsNullOrWhiteSpace(isbn))
            {
                var searchResult = await comicSearchService.SearchByIsbnAsync(isbn, cancellationToken);
                if (searchResult.Found)
                {
                    serie = searchResult.Serie;
                    title = searchResult.Title;
                    authors = searchResult.Authors;
                    publishers = searchResult.Publishers;
                    publishDate = searchResult.PublishDate;
                    imageLink = searchResult.ImageUrl;
                    volumeNumber = searchResult.VolumeNumber;
                }
            }

            // Compute final page count from conversion result
            var pageCount = convertResult.Value!.ProcessedCount + convertResult.Value.SkippedCount;

            // Write updated ComicInfo.xml into converted dir
            var updatedComicInfo = new ComicInfoData(
                Title: title,
                Series: serie,
                Number: volumeNumber,
                Summary: null,
                Year: publishDate?.Year,
                Month: publishDate?.Month,
                Day: publishDate?.Day,
                Writer: authors,
                Penciller: null,
                Publisher: publishers,
                Isbn: isbn,
                PageCount: pageCount > 0 ? pageCount : null);

            var comicInfoDestPath = Path.Combine(convertedDir, "ComicInfo.xml");
            var writeXmlResult = comicInfoXmlService.Write(comicInfoDestPath, updatedComicInfo);
            if (writeXmlResult.IsFailure)
            {
                return await FailJobAsync(importJob, "SearchingMetadata", writeXmlResult.Error!, cancellationToken);
            }

            // ── Step 5: UploadingCover ────────────────────────────────────────
            importJob.Advance(ImportJobStatus.UploadingCover);
            importJobRepository.Update(importJob);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var coverFiles = Directory.Exists(convertedDir)
                ? Directory.GetFiles(convertedDir, "*.webp")
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : [];

            if (coverFiles.Count > 0)
            {
                var publicId = string.IsNullOrWhiteSpace(isbn)
                    ? $"digital-{importJob.Id}"
                    : isbn;

                var uploadResult = await cloudinaryService.UploadImageFromFileAsync(
                    coverFiles[0], "digital-covers", publicId, cancellationToken);

                if (uploadResult.Success && uploadResult.Url is not null)
                {
                    imageLink = uploadResult.Url.ToString();
                }
                else
                {
                    Log.Warning("Cover upload failed for job {JobId}: {Error}", importJob.Id, uploadResult.Error);
                }
            }

            // ── Step 6: BuildingArchive ───────────────────────────────────────
            importJob.Advance(ImportJobStatus.BuildingArchive);
            importJobRepository.Update(importJob);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var outputFileName = string.IsNullOrWhiteSpace(isbn)
                ? $"{importJob.Id}.cbz"
                : $"{isbn}.cbz";
            var outputPath = Path.Combine(tempDir, outputFileName);

            var buildResult = await archiveBuilder.BuildAsync(convertedDir, outputPath, cancellationToken);
            if (buildResult.IsFailure)
            {
                return await FailJobAsync(importJob, "BuildingArchive", buildResult.Error!, cancellationToken);
            }

            // Move CBZ into library directory
            var libraryDir = Path.Combine(libraryLocalStorage.rootPath, library.RelativePath);
            Directory.CreateDirectory(libraryDir);
            var finalPath = Path.Combine(libraryDir, outputFileName);
            File.Move(outputPath, finalPath, overwrite: true);

            // ── Step 7: Completed — create DigitalBook ────────────────────────
            var normalizedIsbn = string.IsNullOrWhiteSpace(isbn)
                ? $"IMPORT-{importJob.Id}"
                : IsbnHelper.NormalizeIsbn(isbn);

            var bookResult = DigitalBook.Create(
                serie, title, normalizedIsbn, importJob.LibraryId,
                finalPath, buildResult.Value!.FileSize,
                volumeNumber, imageLink, authors, publishers,
                publishDate, pageCount > 0 ? pageCount : null);

            if (bookResult.IsFailure)
            {
                return await FailJobAsync(importJob, "Completed", bookResult.Error!, cancellationToken);
            }

            var digitalBook = bookResult.Value!;
            bookRepository.Add(digitalBook);

            importJob.Complete(digitalBook.Id);
            importJobRepository.Update(importJob);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return digitalBook;
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
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
        if (!Directory.Exists(tempDir)) { return; }
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
