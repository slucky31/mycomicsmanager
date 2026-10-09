using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Persistence.Services;

// Shared pipeline of the WebP converters: page order, naming, skip of pages already at the target width,
// progress and ComicInfo.xml. Derived classes only decode, resize and encode one page.
public abstract class WebpImageProcessorBase(ILogger logger) : IImageProcessor
{
    // Pages bigger than this are refused: decoding them would exhaust the memory of the Raspberry Pi.
    protected const int MaxDimension = 8_000;

    private static readonly HashSet<string> s_inputExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp" };

    protected readonly record struct ImageSize(int Width, int Height)
    {
        public bool IsDoublePage => Width > Height;
    }

    // Size of the image without decoding its pixels; null when the file is not a readable image.
    protected abstract Task<ImageSize?> IdentifyAsync(string filePath, CancellationToken ct);

    protected abstract Task ConvertToWebpAsync(string sourcePath, string outputPath, int targetWidth, CancellationToken ct);

    protected static int EffectiveWidth(ImageSize size, int targetWidth) => size.IsDoublePage ? targetWidth * 2 : targetWidth;

    protected static void EnsureSupportedDimensions(ImageSize size)
    {
        if (size.Width > MaxDimension || size.Height > MaxDimension)
        {
            throw new InvalidOperationException(
                $"Image dimensions ({size.Width}×{size.Height}) exceed maximum allowed ({MaxDimension}px).");
        }
    }

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

        var (processedCount, skippedCount) = processResult.Value;
        CopyComicInfoXml(sourceDirectory, destinationDirectory);

        logger.LogInformation(
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

    private async Task<Result<(int ProcessedCount, int SkippedCount)>> ProcessAllImagesAsync(
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

    private async Task<bool> ShouldSkipConversionAsync(string filePath, int targetWidth, CancellationToken ct)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".webp", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var size = await IdentifyAsync(filePath, ct);
            return size is { } s && s.Width == EffectiveWidth(s, targetWidth);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not identify image {FilePath}, proceeding with full conversion", filePath);
            return false;
        }
    }

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
