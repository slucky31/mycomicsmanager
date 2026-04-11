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

        using var fileStream = File.OpenRead(archivePath);
        using var archive = ArchiveFactory.OpenArchive(fileStream, new ReaderOptions { LookForHeader = true });

        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory)
            {
                continue;
            }

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

            var destFilePath = Path.Combine(destinationPath, entryName);
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
