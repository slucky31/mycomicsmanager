using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace Persistence.Services;

public class PdfImageExtractorService : IPdfImageExtractor
{
    private readonly ILogger<PdfImageExtractorService> _logger;
    private readonly ExtractionLimits _limits;

    public PdfImageExtractorService(ILogger<PdfImageExtractorService> logger)
        : this(logger, ExtractionLimits.Default)
    {
    }

    internal PdfImageExtractorService(ILogger<PdfImageExtractorService> logger, ExtractionLimits limits)
    {
        _logger = logger;
        _limits = limits;
    }

    public bool CanHandle(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<Result<PdfExtractionResult>> ExtractImagesAsync(
        string pdfPath,
        string destinationPath,
        CancellationToken ct = default)
    {
        if (!File.Exists(pdfPath))
        {
            _logger.LogWarning("PDF not found: {Path}", pdfPath);
            return FileProcessingError.FileNotFound;
        }

        try
        {
            Directory.CreateDirectory(destinationPath);
            return await Task.Run(() => ExtractInternal(pdfPath, destinationPath, ct), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to extract images from PDF: {Path}", pdfPath);
            return FileProcessingError.CorruptArchive;
        }
    }

    private Result<PdfExtractionResult> ExtractInternal(
        string pdfPath,
        string destinationPath,
        CancellationToken ct)
    {
        using var document = PdfDocument.Open(pdfPath);
        if (document.NumberOfPages > _limits.MaxEntries)
        {
            _logger.LogWarning("PDF with {Count} pages refused", document.NumberOfPages);
            return FileProcessingError.ExtractionLimitExceeded;
        }

        var remainingBytes = Math.Min(_limits.MaxBytes, ExtractionLimits.PdfSizeFactor * new FileInfo(pdfPath).Length);
        var extractedPaths = new List<string>();

        foreach (var page in document.GetPages())
        {
            foreach (var image in page.GetImages())
            {
                ct.ThrowIfCancellationRequested();

                var rawBytes = image.RawBytes;
                remainingBytes -= rawBytes.Length;
                if (extractedPaths.Count >= _limits.MaxEntries || remainingBytes < 0)
                {
                    // An image shown on every page is written once per page: a small PDF could fill the disk.
                    _logger.LogWarning("PDF holds more images or image bytes than an import allows: refused");
                    return FileProcessingError.ExtractionLimitExceeded;
                }

                var bytes = rawBytes.ToArray();
                var fileName = $"page-{extractedPaths.Count + 1:D3}{DetermineExtension(bytes)}";
                var filePath = Path.Combine(destinationPath, fileName);

                File.WriteAllBytes(filePath, bytes);
                extractedPaths.Add(filePath);
            }
        }

        _logger.LogInformation("Extracted {Count} images from PDF", extractedPaths.Count);
        return new PdfExtractionResult(extractedPaths, extractedPaths.Count);
    }

    internal static string DetermineExtension(byte[] rawBytes)
    {
        if (rawBytes.Length >= 2 && rawBytes[0] == 0xFF && rawBytes[1] == 0xD8)
        {
            return ".jpg";
        }

        if (rawBytes.Length >= 4 &&
            rawBytes[0] == 0x89 && rawBytes[1] == 0x50 &&
            rawBytes[2] == 0x4E && rawBytes[3] == 0x47)
        {
            return ".png";
        }

        return ".jpg";
    }
}
