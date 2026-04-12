using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Persistence.Services;

public class ArchiveExtractorService : IArchiveExtractor
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ArchiveExtractorService>();

    private static readonly HashSet<string> s_supportedArchives =
        new(StringComparer.OrdinalIgnoreCase) { ".cbz", ".zip", ".cbr", ".rar" };

    private static readonly HashSet<string> s_allowedFileExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp", ".xml" };

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
            Log.Warning("Archive not found: {Path}", archivePath);
            return FileProcessingError.FileNotFound;
        }

        try
        {
            Directory.CreateDirectory(destinationPath);
            return await Task.Run(() => ExtractInternal(archivePath, destinationPath), ct);
        }
        catch (InvalidDataException ex)
        {
            Log.Error(ex, "Corrupt archive: {Path}", archivePath);
            return FileProcessingError.CorruptArchive;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to extract archive: {Path}", archivePath);
            return FileProcessingError.ProcessingFailed;
        }
    }

    private static Result<ArchiveExtractionResult> ExtractInternal(string archivePath, string destinationPath)
    {
        var extractedFiles = new List<string>();
        string? comicInfoXmlPath = null;
        var canonicalDest = Path.GetFullPath(destinationPath);

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

        foreach (var entry in relevantEntries)
        {
            var entryName = Path.GetFileName(entry.Key ?? string.Empty);
            if (string.IsNullOrEmpty(entryName))
            {
                continue;
            }

            var ext = Path.GetExtension(entryName);
            if (!s_allowedFileExtensions.Contains(ext))
            {
                continue;
            }

            var destFilePath = Path.GetFullPath(Path.Combine(destinationPath, entryName));
            if (!destFilePath.StartsWith(canonicalDest + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && destFilePath != canonicalDest)
            {
                Log.Warning("Path traversal attempt blocked: {Entry}", entry.Key);
                continue;
            }

            entry.WriteToFile(destFilePath, new ExtractionOptions { Overwrite = true });
            extractedFiles.Add(destFilePath);

            if (entryName.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase))
            {
                comicInfoXmlPath = destFilePath;
            }
        }

        var sortedImageFiles = extractedFiles
            .Where(f => !f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Log.Information("Extracted {Count} files from archive, ComicInfo.xml: {HasXml}",
            sortedImageFiles.Count, comicInfoXmlPath is not null);

        return new ArchiveExtractionResult(sortedImageFiles, comicInfoXmlPath);
    }
}
