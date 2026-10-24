using Application.Interfaces;
using Application.Libraries.Summaries;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries;

// includeHidden: false leaves out the hidden libraries (#1215).
public interface ILibraryReadService
{
    Task<IPagedList<Library>> GetLibrariesAsync(
        string? searchTerm,
        LibrariesColumn? sortColumn,
        SortOrder? sortOrder,
        int page,
        int pageSize,
        Guid userId,
        bool includeHidden,
        CancellationToken cancellationToken = default);

    Task<Library?> GetByNameAsync(
        string name,
        Guid userId,
        CancellationToken cancellationToken = default);

    // Every library of the user, ordered by name, with its book count and its latest covers.
    Task<IReadOnlyList<LibrarySummary>> GetSummariesAsync(
        Guid userId,
        bool includeHidden,
        CancellationToken cancellationToken = default);

    // The ids of the hidden libraries of the user.
    Task<IReadOnlySet<Guid>> ListHiddenIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    // The libraries of every user with this name (case-insensitive), for a change made at startup.
    Task<IReadOnlyList<Library>> ListByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid userId,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
