using Domain.Primitives;

namespace Application.Interfaces;

public record ComicPage(ReadOnlyMemory<byte> Content, string ContentType, DateTime LastModifiedUtc);

/// <summary>
/// Reads the pages of a comic archive on demand, straight from the archive:
/// nothing is extracted to disk.
/// </summary>
public interface IComicPageReader
{
    Task<Result<int>> CountPagesAsync(string archivePath, CancellationToken ct = default);

    Task<Result<ComicPage>> ReadPageAsync(string archivePath, int pageIndex, CancellationToken ct = default);
}
