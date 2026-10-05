using Application.Books;
using Application.Books.List;
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

    Task<IReadOnlyList<BookIdentityDto>> ListIdentitiesByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Serie of every book of the user, with its library: used to suggest where a book belongs.
    Task<IReadOnlyList<BookSerieLocationDto>> ListSerieLocationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
