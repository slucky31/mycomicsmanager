using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.List;
public record GetLibrariesQuery(
    string? searchTerm,
    LibrariesColumn? sortColumn,
    SortOrder? sortOrder,
    int page,
    int pageSize) : IQuery<IPagedList<Library>>;

