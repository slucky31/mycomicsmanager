using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Users;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Users;

// Source : https://www.youtube.com/watch?v=X8zRvXbirMU

namespace Application.Libraries.List;

public sealed class GetLibrariesQueryHandler(ILibraryReadService libraryReadService, IRepository<User, Guid> userRepository) : IQueryHandler<GetLibrariesQuery, IPagedList<Library>>
{
    public async Task<Result<IPagedList<Library>>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return LibrariesError.ValidationError;
        }

        var includeHidden = await userRepository.ShowsHiddenLibrariesAsync(request.UserId);
        var list = await libraryReadService.GetLibrariesAsync(request.searchTerm, request.sortColumn, request.sortOrder, request.page, request.pageSize, new UserLibraries(request.UserId, includeHidden), cancellationToken);

        return Result<IPagedList<Library>>.Success(list);
    }
}
