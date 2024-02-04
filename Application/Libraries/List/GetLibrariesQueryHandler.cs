using System.Linq.Expressions;
using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.List;
using Domain.Libraries;
using MediatR;

// Source : https://www.youtube.com/watch?v=X8zRvXbirMU

namespace Persistence.Queries.Libraries;
internal sealed class GetLibrariesQueryHandler : IRequestHandler<GetLibrariesQuery, IPagedList<Library>>
{
    private readonly ILibraryReadService _libraryReadService;

    public GetLibrariesQueryHandler(ILibraryReadService libraryReadService)
    {
        _libraryReadService = libraryReadService;
    }

    public async Task<IPagedList<Library>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
    {
        return await _libraryReadService.GetLibrariesAsync(request.SearchTerm, request.SortColumn, request.SortOrder, request.Page, request.PageSize);
    }
}
