using Domain.Primitives;

namespace Web.Services;

public interface IArchiveRefreshService
{
    /// <summary>Reads again, in the background, the cover and the pages of the current user's digital books from their archive.</summary>
    /// <returns>The number of books to read.</returns>
    Task<Result<int>> StartAsync(CancellationToken cancellationToken = default);
}
