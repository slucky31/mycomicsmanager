using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Users;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Users;

namespace Application.Libraries.Summaries;

public sealed class GetLibrarySummariesQueryHandler(ILibraryReadService libraryReadService, IRepository<User, Guid> userRepository)
    : IQueryHandler<GetLibrarySummariesQuery, IReadOnlyList<LibrarySummary>>
{
    public async Task<Result<IReadOnlyList<LibrarySummary>>> Handle(GetLibrarySummariesQuery request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return LibrariesError.ValidationError;
        }

        var includeHidden = request.IncludeHidden || await userRepository.ShowsHiddenLibrariesAsync(request.UserId);
        var summaries = await libraryReadService.GetSummariesAsync(request.UserId, includeHidden, cancellationToken);

        return Result<IReadOnlyList<LibrarySummary>>.Success(summaries);
    }
}
