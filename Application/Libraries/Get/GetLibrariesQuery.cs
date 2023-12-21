using System.Runtime.CompilerServices;
using Application.Helpers;
using Application.Libraries.GetById;
using Domain.Dto;
using MediatR;

namespace Application.Libraries.Get;
public record GetLibrariesQuery(
    string? SearchTerm, 
    string? SortColumn, 
    string? SortOrder, 
    int Page, 
    int PageSize) : IRequest<PagedList<LibraryDto>>;
