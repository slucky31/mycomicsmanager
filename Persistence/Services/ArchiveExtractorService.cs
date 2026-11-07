using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Persistence.LocalStorage;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Persistence.Services;

public class ArchiveExtractorService(ILogger<ArchiveExtractorService> logger) : IArchiveExtractor
{
    private static readonly HashSet<string> s_supportedArchives =
        new(StringComparer.OrdinalIgnoreCase) { ".cbz", ".zip", ".cbr", ".rar" };

    private static readonly HashSet<string> s_allowedFileExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp", ".xml" };

    private static readonly HashSet<string> s_imageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp" };

    private static readonly HashSet<string> s_nestedArchiveExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".cbz", ".zip", ".cbr", ".rar", ".pdf" };

    private const int MaxArchiveEntries = 5_000;
    private const long MaxUncompressedBytes = 5L * 1024 * 1024 * 1024; // 5 GB

    public bool CanHandle(string filePath) =>
        s_supportedArchives.Contains(Path.GetExtension(filePath));

    public async Task<Result<ArchiveExtractionResult>> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        if (!File.Exists(archivePath))
        {
            logger.LogWarning("Archive not found: {Path}", archivePath);
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
            logger.LogError(ex, "Corrupt archive: {Path}", archivePath);
            return FileProcessingError.CorruptArchive;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to extract archive: {Path}", archivePath);
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

        if (relevantEntries.Count > MaxArchiveEntries)
        {
            return FileProcessingError.CorruptArchive;
        }

        var totalUncompressedSize = relevantEntries.Sum(e => e.Size);
        if (totalUncompressedSize > MaxUncompressedBytes)
        {
            return FileProcessingError.CorruptArchive;
        }

        var pageEntries = relevantEntries.Where(e => HasExtension(e, s_allowedFileExtensions)).ToList();
        if (!pageEntries.Exists(e => HasExtension(e, s_imageExtensions)))
        {
            // No page: several volumes downloaded as one archive. Their archives are extracted to be imported one by one.
            var nestedEntries = relevantEntries.Where(e => HasExtension(e, s_nestedArchiveExtensions)).ToList();
            if (nestedEntries.Count > 0)
            {
                // Sorted by name, so the volumes are listed and imported in their order.
                var nestedArchives = WriteEntries(nestedEntries, destinationPath, normalizedDest)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                logger.LogInformation("Archive without pages, {Count} archives found inside", nestedArchives.Count);
                return new ArchiveExtractionResult([], null) { NestedArchives = nestedArchives };
            }
        }

        var extractedFiles = WriteEntries(pageEntries, destinationPath, normalizedDest);
        var comicInfoXmlPath = extractedFiles.LastOrDefault(f =>
            Path.GetFileName(f).Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase));

        var sortedImageFiles = extractedFiles
            .Where(f => !f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        logger.LogInformation("Extracted {Count} files from archive, ComicInfo.xml: {HasXml}",
            sortedImageFiles.Count, comicInfoXmlPath is not null);

        return new ArchiveExtractionResult(sortedImageFiles, comicInfoXmlPath);
    }

    // Hidden files (e.g. macOS "._" resource forks) are not pages nor volumes.
    private static bool HasExtension(IArchiveEntry entry, HashSet<string> extensions)
    {
        var entryName = Path.GetFileName(entry.Key ?? string.Empty);
        return !string.IsNullOrEmpty(entryName)
            && !entryName.StartsWith('.')
            && extensions.Contains(Path.GetExtension(entryName));
    }

    private List<string> WriteEntries(List<IArchiveEntry> entries, string destinationPath, string normalizedDest)
    {
        var writtenFiles = new List<string>();
        foreach (var entry in entries)
        {
            var entryName = Path.GetFileName(entry.Key ?? string.Empty);
            var destFilePath = Path.GetFullPath(Path.Combine(destinationPath, entryName));
            if (!PathContainment.IsWithinNormalizedRoot(normalizedDest, destFilePath, StringComparison.Ordinal))
            {
                logger.LogWarning("Path traversal attempt blocked: {Entry}", entry.Key);
                continue;
            }

            // PreserveFileTime disabled: NTFS mounted via ntfs-3g only lets the fstab-configured
            // uid change file timestamps, so setting mtime throws EPERM for the app's process.
            entry.WriteToFile(destFilePath, new ExtractionOptions { Overwrite = true, PreserveFileTime = false });
            if (!writtenFiles.Contains(destFilePath))
            {
                writtenFiles.Add(destFilePath);
            }
        }

        return writtenFiles;
    }
}
