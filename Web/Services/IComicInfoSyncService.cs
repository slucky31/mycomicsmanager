using Domain.Primitives;

namespace Web.Services;

public interface IComicInfoSyncService
{
    /// <summary>Writes, in the background, the metadata of the current user's digital books into their ComicInfo.xml.</summary>
    /// <returns>The number of books to write.</returns>
    Task<Result<int>> StartAsync(CancellationToken cancellationToken = default);
}
