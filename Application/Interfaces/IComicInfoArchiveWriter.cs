using Domain.Primitives;

namespace Application.Interfaces;

public interface IComicInfoArchiveWriter
{
    /// <summary>
    /// Writes the metadata of a book into the ComicInfo.xml of its CBZ archive. The elements the book owns are
    /// set, or removed when the book has no value; the other elements of an existing ComicInfo.xml are kept.
    /// An archive that is not a CBZ is left untouched.
    /// </summary>
    Task<Result> WriteAsync(string archivePath, ComicInfoData data, CancellationToken ct = default);
}
