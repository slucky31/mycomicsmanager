using Domain.Primitives;

namespace Application.Interfaces;

public record ComicArchiveResult(
    string ArchivePath,
    long FileSize,
    int PageCount);

public interface IComicArchiveBuilder
{
    /// <summary>
    /// Builds a CBZ (ZIP) archive from an already-enumerated list of WebP images
    /// and an optional ComicInfo.xml file.
    /// </summary>
    Task<Result<ComicArchiveResult>> BuildAsync(
        IReadOnlyList<string> webpFiles,
        string? comicInfoXmlPath,
        string outputPath,
        CancellationToken ct = default);
}
