using Domain.Primitives;

namespace Application.Interfaces;

public record ArchiveExtractionResult(
    IReadOnlyList<string> ExtractedFiles,
    string? ComicInfoXmlPath);

public interface IArchiveExtractor
{
    /// <summary>
    /// Extracts all images and XML files from an archive (zip/cbz/rar/cbr) to the destination directory.
    /// </summary>
    Task<Result<ArchiveExtractionResult>> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken ct = default);

    bool CanHandle(string filePath);
}
