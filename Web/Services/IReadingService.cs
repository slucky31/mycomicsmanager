using Application.Books.Reading;
using Application.Interfaces;
using Domain.Primitives;

namespace Web.Services;

// The books in progress and the last readings of the current user (#1252).
public interface IReadingService
{
    Task<Result<IReadOnlyList<BookInProgressDto>>> GetInProgress(CancellationToken cancellationToken = default);
    Task<Result<IPagedList<BookReadingDto>>> GetRecentReadings(int page, int pageSize, CancellationToken cancellationToken = default);
}
