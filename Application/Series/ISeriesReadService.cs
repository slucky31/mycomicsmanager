using Domain.Series;

namespace Application.Series;

public interface ISeriesReadService
{
    // The books of the user, of one library when given, ordered by volume.
    Task<IReadOnlyList<SerieBookRow>> ListBooksAsync(Guid userId, Guid? libraryId, CancellationToken cancellationToken = default);

    // What the user told of the series of their libraries, of one library when given.
    Task<IReadOnlyList<SeriesInfo>> ListInfosAsync(Guid userId, Guid? libraryId, CancellationToken cancellationToken = default);
}
