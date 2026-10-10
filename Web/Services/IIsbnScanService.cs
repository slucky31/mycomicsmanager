using Application.Books.IsbnScan;
using Domain.Primitives;

namespace Web.Services;

public interface IIsbnScanService
{
    /// <summary>Starts, in the background, the ISBN scan of the books of the library that have none.</summary>
    /// <returns>The number of books to scan.</returns>
    Task<Result<int>> StartLibraryScanAsync(Guid libraryId, CancellationToken cancellationToken = default);

    /// <summary>Reads the pages of one book right away, even if they were already scanned.</summary>
    Task<Result<IsbnScanOutcome>> ScanBookAsync(Guid bookId, CancellationToken cancellationToken = default);
}
