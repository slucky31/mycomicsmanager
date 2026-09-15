using Domain.Primitives;

namespace Application.Interfaces;

public record PdfExtractionResult(
    IReadOnlyList<string> ExtractedImagePaths,
    int PageCount);

public interface IPdfImageExtractor
{
    /// <summary>
    /// Extracts embedded images from each page of a PDF file to the destination directory.
    /// </summary>
    Task<Result<PdfExtractionResult>> ExtractImagesAsync(
        string pdfPath,
        string destinationPath,
        CancellationToken ct = default);

    bool CanHandle(string filePath);
}
