using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

// Source : https://www.youtube.com/watch?v=X8zRvXbirMU

namespace Application.Libraries.List;

public sealed class GetLibrariesQueryHandler(ILibraryReadService libraryReadService) : IQueryHandler<GetLibrariesQuery, IPagedList<Library>>
{
    public async Task<Result<IPagedList<Library>>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return LibrariesError.ValidationError;
        }

        var list = await libraryReadService.GetLibrariesAsync(request.searchTerm, request.sortColumn, request.sortOrder, request.page, request.pageSize, cancellationToken);

        return Result<IPagedList<Library>>.Success(list);
    }
}
