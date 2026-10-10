using Domain.Primitives;

namespace Web.Services;

public interface IIsbnScanService
{
    /// <summary>Starts, in the background, the ISBN scan of the books of the library that have none.</summary>
    /// <returns>The number of books to scan.</returns>
    Task<Result<int>> StartLibraryScanAsync(Guid libraryId, CancellationToken cancellationToken = default);
}
