using System.Linq.Expressions;
using Application.Interfaces;
using Application.Libraries.List;
using Application.Libraries.ReadService;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;

// Source : https://www.youtube.com/watch?v=X8zRvXbirMU

namespace Persistence.Queries.Libraries;
internal sealed class GetLibrariesQueryHandler(ILibraryReadService libraryReadService) : IRequestHandler<GetLibrariesQuery, IPagedList<Library>>
{
    public async Task<IPagedList<Library>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
    {
        return await libraryReadService.GetLibrariesAsync(request.searchTerm, request.sortColumn, request.sortOrder, request.page, request.pageSize);

    }
}
