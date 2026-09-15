using Application.Interfaces;
using Domain.Errors;
using Domain.Primitives;
using UglyToad.PdfPig;

namespace Persistence.Services;

public class PdfImageExtractorService : IPdfImageExtractor
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<PdfImageExtractorService>();

    public bool CanHandle(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<Result<PdfExtractionResult>> ExtractImagesAsync(
        string pdfPath,
        string destinationPath,
        CancellationToken ct = default)
    {
        if (!File.Exists(pdfPath))
        {
            Log.Warning("PDF not found: {Path}", pdfPath);
            return FileProcessingError.FileNotFound;
        }

        try
        {
            Directory.CreateDirectory(destinationPath);
            return await Task.Run(() => ExtractInternal(pdfPath, destinationPath, ct), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to extract images from PDF: {Path}", pdfPath);
            return FileProcessingError.CorruptArchive;
        }
    }

    private static Result<PdfExtractionResult> ExtractInternal(
        string pdfPath,
        string destinationPath,
        CancellationToken ct)
    {
        using var document = PdfDocument.Open(pdfPath);
        var extractedPaths = new List<string>();
        var pageIndex = 1;

        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();

            foreach (var image in page.GetImages())
            {
                var rawBytes = image.RawBytes.ToArray();
                var extension = DetermineExtension(rawBytes);
                var fileName = $"page-{pageIndex:D3}{extension}";
                var filePath = Path.Combine(destinationPath, fileName);

                File.WriteAllBytes(filePath, rawBytes);
                extractedPaths.Add(filePath);
                pageIndex++;
            }
        }

        Log.Information("Extracted {Count} images from PDF", extractedPaths.Count);
        return new PdfExtractionResult(extractedPaths, extractedPaths.Count);
    }

    private static string DetermineExtension(byte[] rawBytes)
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
