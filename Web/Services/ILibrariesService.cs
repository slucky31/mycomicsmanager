using System.Runtime.CompilerServices;
using Application.Interfaces;
using Application.Libraries.Summaries;
using Domain.Libraries;
using Domain.Primitives;

// Nécessaire pour que l'on puisse utiliser NSubstitute dans les tests unitaires (Web.Tests)
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Web.Services;

public interface ILibrariesService
{
    Task<Result<Library>> Create(CreateLibraryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Library>> GetById(string? id);

    Task<Result<Library>> Update(UpdateLibraryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IPagedList<Library>>> FilterBy(string? searchTerm,
        LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LibrarySummary>>> GetSummaries(
        CancellationToken cancellationToken = default);

    // includeHidden lists the hidden libraries even when the user does not show them (#1215).
    Task<Result<IReadOnlyList<LibrarySummary>>> GetSummaries(bool includeHidden,
        CancellationToken cancellationToken = default);

    Task<Result> Delete(string? id,
        CancellationToken cancellationToken = default);
}
