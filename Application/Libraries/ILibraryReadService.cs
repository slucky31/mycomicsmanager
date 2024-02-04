using Application.Interfaces;
using Domain.Libraries;

namespace Application.Libraries;
public interface ILibraryReadService
{
    Task<IPagedList<Library>> GetLibrariesAsync(string? SearchTerm, string? SortColumn, string? SortOrder, int Page, int PageSize);

}
