using Application.Books;
using Application.Books.List;
using Application.Books.Reading;
using Application.Books.Search;
using Domain.Libraries;

namespace Application.Interfaces;

public interface IBookReadService
{
    Task<IPagedList<BookSummaryDto>> GetPagedByLibraryAsync(
        Guid libraryId,
        Guid userId,
        int page,
        int pageSize,
        BookSortOrder sortOrder,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    // The series and the first books of all the libraries of the user whose title or series holds the term (#1209).
    Task<BookSearchResult> SearchAsync(Guid userId, string searchTerm, int bookLimit, bool includeHidden, CancellationToken cancellationToken = default);

    // The digital books opened in the reader and not finished, the last one read first (#1252).
    Task<IReadOnlyList<BookInProgressDto>> ListInProgressAsync(Guid userId, bool includeHidden, CancellationToken cancellationToken = default);

    // The reading dates of the books of the user, the last one first (#1252).
    Task<IPagedList<BookReadingDto>> GetRecentReadingsAsync(Guid userId, int page, int pageSize, bool includeHidden, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookIdentityDto>> ListIdentitiesByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Serie of every book of the user, with its library: used to suggest where a book belongs.
    Task<IReadOnlyList<BookSerieLocationDto>> ListSerieLocationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
