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
}
