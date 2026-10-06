using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Persistence.Services;

// SkiaSharp implementation, designed for the memory of a Raspberry Pi:
// - a JPEG is decoded directly at a reduced scale (1/2, 1/4, 1/8) when it is at least twice the target,
//   so the full-size bitmap is never allocated. The other scales libjpeg offers (3/8, 5/8, 6/8...) are not
//   used: they blur and alias line art far more than a resize does;
// - the remaining reduction is done in halving steps while the ratio exceeds 2×, then one high-quality resize;
// - every page is encoded as lossy WebP, PNG sources included.
public class SkiaImageProcessorService(ILogger<SkiaImageProcessorService> logger) : WebpImageProcessorBase(logger)
{
    // Same quality as the ImageSharp WebP encoder default, so both engines produce comparable files.
    public const int WebpQuality = 75;

    private static readonly SKSamplingOptions s_stepSampling = new(SKFilterMode.Linear, SKMipmapMode.None);
    private static readonly SKSamplingOptions s_finalSampling = new(SKCubicResampler.Mitchell);
    private static readonly int[] s_decodeDenominators = [8, 4, 2];

    protected override Task<ImageSize?> IdentifyAsync(string filePath, CancellationToken ct)
    {
        using var codec = OpenCodec(filePath);
        return Task.FromResult<ImageSize?>(codec is null ? null : new ImageSize(codec.Info.Width, codec.Info.Height));
    }

    protected override Task ConvertToWebpAsync(string sourcePath, string outputPath, int targetWidth, CancellationToken ct)
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
        return Task.CompletedTask;
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
        var current = source;
        try
        {
            // Halving steps keep the quality of a large reduction without the cost of a single cubic resample.
            while (current.Width >= targetWidth * 2)
            {
                var half = Resize(current, current.Width / 2, Math.Max(targetHeight, current.Height / 2), s_stepSampling);
                if (!ReferenceEquals(current, source))
                {
                    current.Dispose();
                }
                current = half;
            }

            var final = Resize(current, targetWidth, targetHeight, s_finalSampling);
            if (!ReferenceEquals(current, source))
            {
                current.Dispose();
            }
            return final;
        }
        catch
        {
            if (!ReferenceEquals(current, source))
            {
                current.Dispose();
            }
            throw;
        }
    }

    private static SKBitmap Resize(SKBitmap source, int width, int height, SKSamplingOptions sampling) =>
        source.Resize(new SKImageInfo(width, height, source.ColorType, source.AlphaType), sampling)
        ?? throw new InvalidOperationException($"Resize to {width}×{height} failed");
}
