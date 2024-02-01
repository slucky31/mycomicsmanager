using Application.Helpers;
using Domain.Libraries;
using MediatR;

namespace Application.Libraries.List;
public record GetLibrariesQuery(
    string? SearchTerm, 
    string? SortColumn, 
    string? SortOrder, 
    int Page, 
    int PageSize) : IRequest<PagedList<Library>>;
