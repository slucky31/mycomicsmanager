using System.IO.Compression;
using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Persistence.Services;

public class ComicArchiveBuilderService(ILogger<ComicArchiveBuilderService> logger) : IComicArchiveBuilder
{
    public async Task<Result<ComicArchiveResult>> BuildAsync(
        IReadOnlyList<string> webpFiles,
        string? comicInfoXmlPath,
        string outputPath,
        CancellationToken ct = default)
    {
        if (webpFiles.Count == 0)
        {
            return FileProcessingError.EmptyDirectory;
        }

        var hasComicInfo = comicInfoXmlPath is not null && File.Exists(comicInfoXmlPath);

        try
        {
            await Task.Run(() => CreateArchive(outputPath, webpFiles, hasComicInfo ? comicInfoXmlPath : null), ct);

            var fileInfo = new FileInfo(outputPath);
            logger.LogInformation("Built CBZ archive: {Path} ({Pages} pages, {Size} bytes)",
                outputPath, webpFiles.Count, fileInfo.Length);

            return new ComicArchiveResult(outputPath, fileInfo.Length, webpFiles.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to build CBZ archive: {Output}", outputPath);
            return FileProcessingError.ProcessingFailed;
        }
    }

    private static void CreateArchive(string outputPath, IReadOnlyList<string> webpFiles, string? comicInfoPath)
    {
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);

        foreach (var webpFile in webpFiles)
        {
            archive.CreateEntryFromFile(webpFile, Path.GetFileName(webpFile), CompressionLevel.Fastest);
        }

        if (comicInfoPath is not null)
        {
            archive.CreateEntryFromFile(comicInfoPath, "ComicInfo.xml", CompressionLevel.Fastest);
        }
    }
}
