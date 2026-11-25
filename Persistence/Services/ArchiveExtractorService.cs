using Application.Interfaces;
using Domain.Errors;
using Domain.ImportJobs;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Persistence.LocalStorage;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Persistence.Services;

public class ArchiveExtractorService : IArchiveExtractor
{
    private readonly ILogger<ArchiveExtractorService> _logger;
    private readonly ExtractionLimits _limits;

    public ArchiveExtractorService(ILogger<ArchiveExtractorService> logger)
        : this(logger, ExtractionLimits.Default)
    {
    }

    internal ArchiveExtractorService(ILogger<ArchiveExtractorService> logger, ExtractionLimits limits)
    {
        _logger = logger;
        _limits = limits;
    }

    private static readonly HashSet<string> s_supportedArchives =
        new(StringComparer.OrdinalIgnoreCase) { ".cbz", ".zip", ".cbr", ".rar" };

    private static readonly HashSet<string> s_allowedFileExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp", ".xml" };

    private static readonly HashSet<string> s_imageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp" };

    private static readonly HashSet<string> s_nestedArchiveExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".cbz", ".zip", ".cbr", ".rar", ".pdf" };

    public bool CanHandle(string filePath) =>
        s_supportedArchives.Contains(Path.GetExtension(filePath));

    public async Task<Result<ArchiveExtractionResult>> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        if (!File.Exists(archivePath))
        {
            _logger.LogWarning("Archive not found: {Path}", archivePath);
            return FileProcessingError.FileNotFound;
        }

        try
        {
            Directory.CreateDirectory(destinationPath);
            return await Task.Run(() => ExtractInternal(archivePath, destinationPath), ct);
        }
        // SharpCompress reports unreadable archives (e.g. a ZIP truncated before its central directory)
        // with ArchiveException, IncompleteArchiveException included.
        catch (Exception ex) when (ex is InvalidDataException or ArchiveException)
        {
            _logger.LogError(ex, "Corrupt archive: {Path}", archivePath);
            return FileProcessingError.CorruptArchive;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to extract archive: {Path}", archivePath);
            return FileProcessingError.ProcessingFailed;
        }
    }

    private Result<ArchiveExtractionResult> ExtractInternal(string archivePath, string destinationPath)
    {
        var normalizedDest = PathContainment.NormalizeRoot(destinationPath);

        using var fileStream = File.OpenRead(archivePath);
        using var archive = ArchiveFactory.OpenArchive(fileStream, new ReaderOptions { LookForHeader = true });

        var relevantEntries = archive.Entries
            .Where(e => !e.IsDirectory)
            .ToList();

        if (relevantEntries.Count > _limits.MaxEntries)
        {
            return FileProcessingError.CorruptArchive;
        }

        // Each entry is then written up to its declared size at most: the declared total bounds what is written.
        var totalUncompressedSize = relevantEntries.Sum(e => Math.Max(e.Size, 0));
        if (totalUncompressedSize > _limits.MaxBytes)
        {
            return FileProcessingError.CorruptArchive;
        }

        if (_limits.AvailableBytes(destinationPath) < totalUncompressedSize)
        {
            _logger.LogWarning("Not enough disk space to extract {Bytes} bytes into {Path}", totalUncompressedSize, destinationPath);
            return ImportJobError.InsufficientDiskSpace;
        }

        var pageEntries = relevantEntries.Where(e => HasExtension(e, s_allowedFileExtensions)).ToList();
        if (!pageEntries.Exists(e => HasExtension(e, s_imageExtensions)))
        {
            // No page: several volumes downloaded as one archive. Their archives are extracted to be imported one by one.
            var nestedEntries = relevantEntries.Where(e => HasExtension(e, s_nestedArchiveExtensions)).ToList();
            if (nestedEntries.Count > 0)
            {
                return ExtractNestedArchives(nestedEntries, destinationPath, normalizedDest);
            }
        }

        var written = WriteEntries(pageEntries, destinationPath, normalizedDest);
        if (written.IsFailure)
        {
            return written.Error!;
        }

        var extractedFiles = written.Value!;
        var comicInfoXmlPath = extractedFiles.LastOrDefault(f =>
            Path.GetFileName(f).Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase));

        var sortedImageFiles = extractedFiles
            .Where(f => !f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation("Extracted {Count} files from archive, ComicInfo.xml: {HasXml}",
            sortedImageFiles.Count, comicInfoXmlPath is not null);

        return new ArchiveExtractionResult(sortedImageFiles, comicInfoXmlPath);
    }

    private Result<ArchiveExtractionResult> ExtractNestedArchives(List<IArchiveEntry> nestedEntries, string destinationPath, string normalizedDest)
    {
        var written = WriteEntries(nestedEntries, destinationPath, normalizedDest);
        if (written.IsFailure)
        {
            return written.Error!;
        }

        // Sorted by name, so the volumes are listed and imported in their order.
        var nestedArchives = written.Value!.Order(StringComparer.OrdinalIgnoreCase).ToList();

        // One level only (#1370): each archive found inside is imported again, and would split again.
        var tooDeep = nestedArchives.Find(HoldsOnlyArchives);
        if (tooDeep is not null)
        {
            _logger.LogWarning("Archive {Name} inside the archive holds other archives: refused", Path.GetFileName(tooDeep));
            return FileProcessingError.NestedArchiveTooDeep;
        }

        _logger.LogInformation("Archive without pages, {Count} archives found inside", nestedArchives.Count);
        return new ArchiveExtractionResult([], null) { NestedArchives = nestedArchives };
    }

    // The same rule as ExtractInternal: an archive without any page but with archives would be split.
    private bool HoldsOnlyArchives(string archivePath)
    {
        if (!CanHandle(archivePath))
        {
            return false;
        }

        try
        {
            using var stream = File.OpenRead(archivePath);
            using var archive = ArchiveFactory.OpenArchive(stream, new ReaderOptions { LookForHeader = true });
            var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
            return !entries.Exists(e => HasExtension(e, s_imageExtensions))
                && entries.Exists(e => HasExtension(e, s_nestedArchiveExtensions));
        }
        catch (Exception ex) when (ex is InvalidDataException or ArchiveException or ArchiveOperationException or IOException)
        {
            // An unreadable archive fails later, in its own import.
            return false;
        }
    }

    // Hidden files (e.g. macOS "._" resource forks) are not pages nor volumes.
    private static bool HasExtension(IArchiveEntry entry, HashSet<string> extensions)
    {
        var entryName = Path.GetFileName(entry.Key ?? string.Empty);
        return !string.IsNullOrEmpty(entryName)
            && !entryName.StartsWith('.', StringComparison.Ordinal)
            && extensions.Contains(Path.GetExtension(entryName));
    }

    private Result<List<string>> WriteEntries(List<IArchiveEntry> entries, string destinationPath, string normalizedDest)
    {
        var writtenFiles = new List<string>();
        var remainingBytes = _limits.MaxBytes;
        foreach (var entry in entries)
        {
            var entryName = Path.GetFileName(entry.Key ?? string.Empty);
            var destFilePath = Path.GetFullPath(Path.Combine(destinationPath, entryName));
            if (!PathContainment.IsWithinNormalizedRoot(normalizedDest, destFilePath, StringComparison.Ordinal))
            {
                _logger.LogWarning("Path traversal attempt blocked: {Entry}", entry.Key);
                continue;
            }

            // A file is overwritten when two entries have the same name. Its time is not preserved: NTFS mounted
            // via ntfs-3g only lets the fstab-configured uid change file timestamps (EPERM for the app's process).
            var maxBytes = Math.Min(Math.Max(entry.Size, 0), remainingBytes);
            long copiedBytes;
            bool complete;
            using (var source = entry.OpenEntryStream())
            using (var destination = new FileStream(destFilePath, FileMode.Create, FileAccess.Write))
            {
                complete = BoundedCopy.TryCopy(source, destination, maxBytes, out copiedBytes);
            }

            if (!complete)
            {
                File.Delete(destFilePath);
                _logger.LogWarning("Entry {Entry} holds more than its declared size of {Size} bytes: refused", entry.Key, entry.Size);
                return FileProcessingError.ExtractionLimitExceeded;
            }

            remainingBytes -= copiedBytes;
            if (!writtenFiles.Contains(destFilePath))
            {
                writtenFiles.Add(destFilePath);
            }
        }

        return writtenFiles;
    }
}
