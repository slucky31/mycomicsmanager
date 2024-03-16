using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.List;
using Domain.Libraries;
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
