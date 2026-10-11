using Application.Abstractions.Messaging;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.Summaries;

public sealed class GetLibrarySummariesQueryHandler(ILibraryReadService libraryReadService)
    : IQueryHandler<GetLibrarySummariesQuery, IReadOnlyList<LibrarySummary>>
{
    public async Task<Result<IReadOnlyList<LibrarySummary>>> Handle(GetLibrarySummariesQuery request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return LibrariesError.ValidationError;
        }

        var summaries = await libraryReadService.GetSummariesAsync(request.UserId, cancellationToken);

        return Result<IReadOnlyList<LibrarySummary>>.Success(summaries);
    }
}
