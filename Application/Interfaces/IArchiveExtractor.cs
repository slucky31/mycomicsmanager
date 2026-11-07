using Domain.Primitives;

namespace Application.Interfaces;

public record ArchiveExtractionResult(
    IReadOnlyList<string> ExtractedFiles,
    string? ComicInfoXmlPath)
{
    // Comic archives (cbz, cbr, zip, rar, pdf) found inside an archive that has no page of its own:
    // several volumes downloaded as one file.
    public IReadOnlyList<string> NestedArchives { get; init; } = [];
}

public interface IArchiveExtractor
{
    /// <summary>
    /// Extracts all images and XML files from an archive (zip/cbz/rar/cbr) to the destination directory.
    /// An archive without any image has its inner comic archives extracted instead (see NestedArchives).
    /// </summary>
    Task<Result<ArchiveExtractionResult>> ExtractAsync(
        string archivePath,
        string destinationPath,
        CancellationToken ct = default);

    bool CanHandle(string filePath);
}
