using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Persistence.Services;

public class ImageProcessorService : IImageProcessor
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ImageProcessorService>();

    private static readonly HashSet<string> s_inputExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp" };

    public async Task<Result<ImageProcessingResult>> ProcessImagesAsync(
        string sourceDirectory,
        string destinationDirectory,
        int targetWidth = 1400,
        Func<ImageConversionProgress, Task>? onProgressAsync = null,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return FileProcessingError.InvalidPath;
        }

        var imageFiles = GetSortedImageFiles(sourceDirectory);

        if (imageFiles.Count == 0)
        {
            return new ImageProcessingResult(0, 0, false);
        }

        Directory.CreateDirectory(destinationDirectory);
        var processResult = await ProcessAllImagesAsync(
            imageFiles, destinationDirectory, targetWidth, onProgressAsync, ct);

        if (processResult.IsFailure)
        {
            return processResult.Error!;
        }

        var (processedCount, skippedCount) = processResult.Value!;
        CopyComicInfoXml(sourceDirectory, destinationDirectory);

        Log.Information(
            "Processed {Converted} images to WebP, skipped {Skipped} already-optimal WebP files",
            processedCount, skippedCount);

        return new ImageProcessingResult(processedCount, skippedCount, processedCount == 0);
    }

    private static List<string> GetSortedImageFiles(string sourceDirectory) =>
        Directory.GetFiles(sourceDirectory)
            .Where(f => !Path.GetFileName(f).StartsWith('.') &&
                        s_inputExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static async Task<Result<(int ProcessedCount, int SkippedCount)>> ProcessAllImagesAsync(
        List<string> imageFiles,
        string destinationDirectory,
        int targetWidth,
        Func<ImageConversionProgress, Task>? onProgressAsync,
        CancellationToken ct)
    {
        var processedCount = 0;
        var skippedCount = 0;

        for (var i = 0; i < imageFiles.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var outputPath = Path.Combine(destinationDirectory, $"page-{i + 1:D3}.webp");

            if (await ShouldSkipConversionAsync(imageFiles[i], targetWidth, ct))
            {
                File.Copy(imageFiles[i], outputPath, overwrite: true);
                skippedCount++;
            }
            else
            {
                try
                {
                    await ConvertToWebpAsync(imageFiles[i], outputPath, targetWidth, ct);
                    processedCount++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return FileProcessingError.InvalidImageContent(Path.GetFileName(imageFiles[i]));
                }
            }

            if (onProgressAsync != null)
            {
                await onProgressAsync(new ImageConversionProgress(i + 1, imageFiles.Count));
            }
        }

        return (processedCount, skippedCount);
    }

    private static async Task<bool> ShouldSkipConversionAsync(string filePath, int targetWidth, CancellationToken ct)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".webp", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var info = await Image.IdentifyAsync(filePath, ct);
            if (info is null)
            {
                return false;
            }

            var isDoublePage = info.Width > info.Height;
            var expectedWidth = isDoublePage ? targetWidth * 2 : targetWidth;
            return info.Width == expectedWidth;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    private static async Task ConvertToWebpAsync(
        string sourcePath,
        string outputPath,
        int targetWidth,
        CancellationToken ct)
    {
        using var image = await Image.LoadAsync(sourcePath, ct);

        const int maxDimension = 8_000;
        if (image.Width > maxDimension || image.Height > maxDimension)
        {
            throw new InvalidOperationException(
                $"Image dimensions ({image.Width}×{image.Height}) exceed maximum allowed ({maxDimension}px).");
        }

        var effectiveWidth = IsDoublePage(image) ? targetWidth * 2 : targetWidth;
        var targetHeight = (int)Math.Round((double)image.Height * effectiveWidth / image.Width);

        image.Mutate(x => x.Resize(effectiveWidth, targetHeight));
        await image.SaveAsWebpAsync(outputPath, new WebpEncoder(), ct);
    }

    private static bool IsDoublePage(Image image) => image.Width > image.Height;

    private static void CopyComicInfoXml(string sourceDirectory, string destinationDirectory)
    {
        var xmlSource = Path.Combine(sourceDirectory, "ComicInfo.xml");
        if (!File.Exists(xmlSource))
        {
            return;
        }

        var xmlDest = Path.Combine(destinationDirectory, "ComicInfo.xml");
        if (!xmlSource.Equals(xmlDest, StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(xmlSource, xmlDest, overwrite: true);
        }
    }
}
