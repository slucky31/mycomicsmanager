using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;

namespace Application.Interfaces;

public interface IBookReadService
{
    Task<IPagedList<Book>> GetPagedByLibraryAsync(
        Guid libraryId,
        Guid userId,
        int page,
        int pageSize,
        BookSortOrder sortOrder,
        string? searchTerm,
        CancellationToken cancellationToken = default);
}
