using System.ComponentModel;
using System.Diagnostics;
using Application.Books.IsbnScan;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

    private static readonly PngEncoder s_grayscaleEncoder = new()
    {
        ColorType = PngColorType.Grayscale,
        BitDepth = PngBitDepth.Bit8,
    };

    private readonly IsbnOcrSettings _settings = options.Value;

    public async Task<Result<string>> RecognizeAsync(ReadOnlyMemory<byte> image, CancellationToken ct = default)
    {
        byte[] png;
        try
        {
            png = await PrepareAsync(image, ct);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            logger.LogWarning(ex, "Page image cannot be decoded for OCR");
            return OcrError.UnreadableImage;
        }

        return await RunTesseractAsync(png, ct);
    }

    // Grayscale and enlarged: what Tesseract reads best.
    private static async Task<byte[]> PrepareAsync(ReadOnlyMemory<byte> image, CancellationToken ct)
    {
        using var page = Image.Load<L8>(image.Span);
        var scale = Math.Min(MaxUpscale, (double)MaxLongSide / Math.Max(page.Width, page.Height));
        if (scale > 1)
        {
            var width = (int)Math.Round(page.Width * scale);
            var height = (int)Math.Round(page.Height * scale);
            page.Mutate(x => x.Resize(width, height, KnownResamplers.Bicubic));
        }

        using var buffer = new MemoryStream();
        await page.SaveAsPngAsync(buffer, s_grayscaleEncoder, ct);
        return buffer.ToArray();
    }

    private async Task<Result<string>> RunTesseractAsync(byte[] png, CancellationToken ct)
    {
        using var process = new Process { StartInfo = CreateStartInfo() };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            logger.LogWarning(ex, "Tesseract cannot be started from {Path}", _settings.TesseractPath);
            return OcrError.Unavailable;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.PageTimeoutSeconds));

        try
        {
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.StandardInput.BaseStream.WriteAsync(png, timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);

            if (process.ExitCode != 0)
            {
                logger.LogWarning("Tesseract failed with exit code {ExitCode}: {Error}", process.ExitCode, await error);
                return OcrError.Failed;
            }

            return await output;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Tesseract did not read the page within {Timeout} s", _settings.PageTimeoutSeconds);
            return OcrError.Timeout;
        }
        catch (IOException ex)
        {
            // Tesseract closed its input before reading the whole image.
            logger.LogWarning(ex, "Tesseract stopped while reading the page");
            return OcrError.Failed;
        }
        finally
        {
            StopIfRunning(process);
        }
    }

    private ProcessStartInfo CreateStartInfo()
    {
        var startInfo = new ProcessStartInfo(_settings.TesseractPath)
        {
            RedirectStandardInput = true,
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

    private static void StopIfRunning(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process already exited.
        }
    }
}
