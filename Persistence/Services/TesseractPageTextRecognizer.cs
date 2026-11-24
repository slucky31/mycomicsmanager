using System.ComponentModel;
using System.Diagnostics;
using Application.Books.IsbnScan;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;
using SkiaSharp;

namespace Persistence.Services;

/// <summary>
/// Runs the Tesseract command line on a page: the image is piped to its standard input and the
/// text read from its standard output, so nothing is written on disk.
/// </summary>
public sealed class TesseractPageTextRecognizer(
    IOptions<IsbnOcrSettings> options,
    ILogger<TesseractPageTextRecognizer> logger) : IPageTextRecognizer
{
    // Small print (the copyright notice) is only readable once enlarged; the cap bounds the OCR time.
    private const double MaxUpscale = 2;
    private const int MaxLongSide = 4096;

    private static readonly SKSamplingOptions s_upscaleSampling = new(SKCubicResampler.CatmullRom);

    private readonly IsbnOcrSettings _settings = options.Value;

    public async Task<Result<string>> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken ct = default)
    {
        var png = Prepare(image);
        if (png is null)
        {
            logger.LogWarning("Page image cannot be decoded for OCR");
            return OcrError.UnreadableImage;
        }

        return await RunTesseractAsync(png, ct);
    }

    // Grayscale and enlarged: what Tesseract reads best. Null when the bytes are not a readable image.
    private static byte[]? Prepare(ReadOnlyMemory<byte> image)
    {
        using var stream = new MemoryStream(image.ToArray(), writable: false);
        using var codec = SKCodec.Create(stream, out var result);
        if (result != SKCodecResult.Success)
        {
            return null;
        }

        using var page = SKBitmap.Decode(codec);
        if (page is null)
        {
            return null;
        }

        var scale = Math.Min(MaxUpscale, (double)MaxLongSide / Math.Max(page.Width, page.Height));
        using var enlarged = scale > 1 ? Enlarge(page, scale) : null;
        // The colour to grayscale conversion is done last: Skia only resamples colour bitmaps.
        using var grayscale = (enlarged ?? page).Copy(SKColorType.Gray8)
            ?? throw new InvalidOperationException("Grayscale conversion failed");
        using var png = grayscale.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("PNG encoding failed");
        return png.ToArray();
    }

    private static SKBitmap Enlarge(SKBitmap page, double scale)
    {
        var width = (int)Math.Round(page.Width * scale, MidpointRounding.ToEven);
        var height = (int)Math.Round(page.Height * scale, MidpointRounding.ToEven);
        return page.Resize(new SKImageInfo(width, height, page.ColorType, page.AlphaType), s_upscaleSampling)
            ?? throw new InvalidOperationException($"Resize to {width}×{height} failed");
    }

    private async Task<Result<string>> RunTesseractAsync(byte[] png, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.PageTimeoutSeconds));

        // The image goes through a pipe: the read end becomes the standard input of Tesseract.
        SafeFileHandle.CreateAnonymousPipe(out var inputRead, out var inputWrite, asyncRead: false, asyncWrite: true);
        Task<ProcessTextOutput> run;
        using (inputRead)
        {
            // On timeout, RunAndCaptureTextAsync kills the process before throwing.
            run = Process.RunAndCaptureTextAsync(CreateStartInfo(inputRead), timeout.Token);
        }

        bool written;
        using (inputWrite)
        {
            written = await WriteInputAsync(inputWrite, png, timeout.Token);
        }

        try
        {
            var result = await run;
            if (!written)
            {
                logger.LogWarning("Tesseract stopped while reading the page");
                return OcrError.Failed;
            }

            if (result.ExitStatus.ExitCode != 0)
            {
                logger.LogWarning("Tesseract failed with exit code {ExitCode}: {Error}", result.ExitStatus.ExitCode, result.StandardError);
                return OcrError.Failed;
            }

            return result.StandardOutput;
        }
        catch (Win32Exception ex)
        {
            logger.LogWarning(ex, "Tesseract cannot be started from {Path}", _settings.TesseractPath);
            return OcrError.Unavailable;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Tesseract did not read the page within {Timeout} s", _settings.PageTimeoutSeconds);
            return OcrError.Timeout;
        }
    }

    // False when Tesseract closed its input (it stopped, or was never started) before reading the whole image.
    private static async Task<bool> WriteInputAsync(SafeFileHandle inputWrite, byte[] png, CancellationToken ct)
    {
        try
        {
            await using var input = new FileStream(inputWrite, FileAccess.Write, bufferSize: 1, isAsync: true);
            await input.WriteAsync(png, ct);
            return true;
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException)
        {
            // A timeout is reported by the process run, which is cancelled by the same token.
            return false;
        }
    }

    private ProcessStartInfo CreateStartInfo(SafeFileHandle input)
    {
        var startInfo = new ProcessStartInfo(_settings.TesseractPath)
        {
            StandardInputHandle = input,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("stdin");
        startInfo.ArgumentList.Add("stdout");
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(_settings.Language);
        // One thread per page: the background jobs already run in parallel.
        startInfo.Environment["OMP_THREAD_LIMIT"] = "1";
        return startInfo;
    }
}
