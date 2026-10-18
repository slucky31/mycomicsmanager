using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Persistence.Services;

// Converts the pages of a comic to WebP with SkiaSharp, designed for the memory of a Raspberry Pi:
// - a JPEG is decoded directly at a reduced scale (1/2, 1/4, 1/8) when it is at least twice the target,
//   so the full-size bitmap is never allocated. The other scales libjpeg offers (3/8, 5/8, 6/8...) are not
//   used: they blur and alias line art far more than a resize does;
// - the remaining reduction is done in halving steps while the ratio exceeds 2×, then one high-quality resize;
// - every page is encoded as lossy WebP, PNG sources included.
// - a page narrower than the target is never enlarged: upscaling adds no detail, and a narrow strip (a spine)
//   would exceed the 16383px limit of WebP once scaled to the target width.
// Pages are named page-001.webp... in file name order; WebP pages already at their final width are copied as is.
public class ImageProcessorService(ILogger<ImageProcessorService> logger) : IImageProcessor
{
    // Same quality as the former ImageSharp converter: measured equivalent on a real album, with smaller files.
    public const int WebpQuality = 75;

    private static readonly SKSamplingOptions s_stepSampling = new(SKFilterMode.Linear, SKMipmapMode.None);
    private static readonly SKSamplingOptions s_finalSampling = new(SKCubicResampler.Mitchell);
    private static readonly int[] s_decodeDenominators = [8, 4, 2];
    // Pages bigger than this are refused: decoding them would exhaust the memory of the Raspberry Pi.
    private const int MaxDimension = 8_000;

    private static readonly HashSet<string> s_inputExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".gif", ".webp" };

    private readonly record struct ImageSize(int Width, int Height)
    {
        public bool IsDoublePage => Width > Height;
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

            if (ShouldSkipConversion(imageFiles[i], targetWidth))
            {
                File.Copy(imageFiles[i], outputPath, overwrite: true);
                skippedCount++;
            }
            else
            {
                try
                {
                    ConvertToWebp(imageFiles[i], outputPath, targetWidth, ct);
                    processedCount++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Could not convert image {FilePath}", imageFiles[i]);
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

    private bool ShouldSkipConversion(string filePath, int targetWidth)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".webp", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var size = Identify(filePath);
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

    private static int EffectiveWidth(ImageSize size, int targetWidth) =>
        Math.Min(size.Width, size.IsDoublePage ? targetWidth * 2 : targetWidth);

    private static void EnsureSupportedDimensions(ImageSize size)
    {
        if (size.Width > MaxDimension || size.Height > MaxDimension)
        {
            throw new InvalidOperationException(
                $"Image dimensions ({size.Width}×{size.Height}) exceed maximum allowed ({MaxDimension}px).");
        }
    }

    // Size of the image without decoding its pixels; null when the file is not a readable image.
    private static ImageSize? Identify(string filePath)
    {
        using var codec = OpenCodec(filePath);
        return codec is null ? null : new ImageSize(codec.Info.Width, codec.Info.Height);
    }

    private static void ConvertToWebp(string sourcePath, string outputPath, int targetWidth, CancellationToken ct)
    {
        using var codec = OpenCodec(sourcePath)
            ?? throw new InvalidDataException($"Unsupported or corrupt image: {Path.GetFileName(sourcePath)}");
        var size = new ImageSize(codec.Info.Width, codec.Info.Height);
        EnsureSupportedDimensions(size);

        var effectiveWidth = EffectiveWidth(size, targetWidth);
        var targetHeight = (int)Math.Round((double)size.Height * effectiveWidth / size.Width);

        using var decoded = Decode(codec, effectiveWidth);
        ct.ThrowIfCancellationRequested();
        using var resized = ResizeInSteps(decoded, effectiveWidth, targetHeight);
        using var data = resized.Encode(SKEncodedImageFormat.Webp, WebpQuality)
            ?? throw new InvalidOperationException($"WebP encoding failed: {Path.GetFileName(sourcePath)}");

        using var output = File.Create(outputPath);
        data.SaveTo(output);
    }

    // Null when the file is not an image Skia can read (SKCodec.Create returns null in that case).
    private static SKCodec? OpenCodec(string filePath)
    {
        var codec = SKCodec.Create(filePath, out var result);
        if (result == SKCodecResult.Success)
        {
            return codec;
        }

        // The binding is annotated non-null, but Skia returns null (or a codec to release) on failure.
#pragma warning disable CA1508
        codec?.Dispose();
#pragma warning restore CA1508
        return null;
    }

    // Largest power-of-two reduction (1/8, 1/4, 1/2) that keeps the decoded width at least equal to the target.
    internal static int DecodeScaleDenominator(int sourceWidth, int targetWidth)
    {
        foreach (var denominator in s_decodeDenominators)
        {
            if ((sourceWidth + denominator - 1) / denominator >= targetWidth)
            {
                return denominator;
            }
        }

        return 1;
    }

    // Only JPEG supports scaled decoding: the other formats return their full size and are decoded as is.
    private static SKBitmap Decode(SKCodec codec, int targetWidth)
    {
        var full = codec.Info;
        var denominator = DecodeScaleDenominator(full.Width, targetWidth);
        var scaled = denominator == 1 ? full.Size : codec.GetScaledDimensions(1f / denominator);
        var decodeSize = scaled.Width >= targetWidth ? scaled : full.Size;

        var info = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
        {
            bitmap.Dispose();
            throw new InvalidDataException($"Image could not be decoded: {result}");
        }

        return bitmap;
    }

    private static SKBitmap ResizeInSteps(SKBitmap source, int targetWidth, int targetHeight)
    {
        // The source belongs to the caller: only the halved copies are released here, even when a resize fails.
        SKBitmap? intermediate = null;
        try
        {
            var current = source;
            // Halving steps keep the quality of a large reduction without the cost of a single cubic resample.
            while (current.Width >= targetWidth * 2)
            {
                var half = Resize(current, current.Width / 2, Math.Max(targetHeight, current.Height / 2), s_stepSampling);
                intermediate?.Dispose();
                intermediate = half;
                current = half;
            }

            return Resize(current, targetWidth, targetHeight, s_finalSampling);
        }
        finally
        {
            intermediate?.Dispose();
        }
    }

    private static SKBitmap Resize(SKBitmap source, int width, int height, SKSamplingOptions sampling) =>
        source.Resize(new SKImageInfo(width, height, source.ColorType, source.AlphaType), sampling)
        ?? throw new InvalidOperationException($"Resize to {width}×{height} failed");
}
