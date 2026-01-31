using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries;

public interface ILibraryReadService
{
    Task<IPagedList<Library>> GetLibrariesAsync(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize, CancellationToken cancellationToken = default);

}
