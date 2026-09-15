using Domain.Primitives;

namespace Application.Interfaces;

public record ComicArchiveResult(
    string ArchivePath,
    long FileSize,
    int PageCount);

public interface IComicArchiveBuilder
{
    /// <summary>
    /// Builds a CBZ (ZIP) archive from a directory containing WebP images
    /// and an optional ComicInfo.xml file.
    /// </summary>
    Task<Result<ComicArchiveResult>> BuildAsync(
        string sourceDirectory,
        string outputPath,
        CancellationToken ct = default);
}
