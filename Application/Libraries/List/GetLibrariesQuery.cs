using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Libraries.List;
public record GetLibrariesQuery(
    string? searchTerm,
    LibrariesColumn? sortColumn,
    SortOrder? sortOrder,
    int page,
    int pageSize) : IRequest<IPagedList<Library>>;

