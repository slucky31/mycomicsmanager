using System.IO.Compression;
using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;

namespace Persistence.Services;

public class ComicArchiveBuilderService : IComicArchiveBuilder
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ComicArchiveBuilderService>();

    public async Task<Result<ComicArchiveResult>> BuildAsync(
        string sourceDirectory,
        string outputPath,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return FileProcessingError.InvalidPath;
        }

        var webpFiles = Directory.GetFiles(sourceDirectory, "*.webp")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (webpFiles.Count == 0)
        {
            return FileProcessingError.EmptyDirectory;
        }

        var comicInfoPath = Path.Combine(sourceDirectory, "ComicInfo.xml");
        var hasComicInfo = File.Exists(comicInfoPath);

        try
        {
            await Task.Run(() => CreateArchive(outputPath, webpFiles, hasComicInfo ? comicInfoPath : null), ct);

            var fileInfo = new FileInfo(outputPath);
            Log.Information("Built CBZ archive: {Path} ({Pages} pages, {Size} bytes)",
                outputPath, webpFiles.Count, fileInfo.Length);

            return new ComicArchiveResult(outputPath, fileInfo.Length, webpFiles.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to build CBZ archive: {Output}", outputPath);
            return FileProcessingError.ProcessingFailed;
        }
    }

    private static void CreateArchive(string outputPath, List<string> webpFiles, string? comicInfoPath)
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
