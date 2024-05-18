using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Web.Services;
public interface ILibrariesService
{
    Task<Result<Library>> Create(string? name);
    Task<Result<Library>> GetById(string? id);
    Task<Result<Library>> Update(string? id, string? name);
    Task<IPagedList<Library>> FilterBy(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize);
    Task<Result> Delete(string? id);
}
