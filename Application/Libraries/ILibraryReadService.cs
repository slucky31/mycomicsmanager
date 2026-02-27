using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries;

public interface ILibraryReadService
{
    Task<IPagedList<Library>> GetLibrariesAsync(
        string? searchTerm,
        LibrariesColumn? sortColumn,
        SortOrder? sortOrder,
        int page,
        int pageSize,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid userId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
