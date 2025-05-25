using System.Runtime.CompilerServices;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

// Nécessaire pour que l'on puisse utiliser NSubstitute dans les tests unitaires (Web.Tests)
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Web.Services;

internal interface ILibrariesService
{
    Task<Result<Library>> Create(string? name);
    Task<Result<Library>> GetById(string? id);
    Task<Result<Library>> Update(string? id, string? name);
    Task<Result<IPagedList<Library>>> FilterBy(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize);
    Task<Result> Delete(string? id);
}
