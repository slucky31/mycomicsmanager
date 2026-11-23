using Application.Abstractions.Messaging;
using Domain.Primitives;

namespace Application.Books.Covers;

public sealed class FindUnusedCoversQueryHandler(UnusedCoverFinder finder) : IQueryHandler<FindUnusedCoversQuery, UnusedCovers>
{
    public async Task<Result<UnusedCovers>> Handle(FindUnusedCoversQuery query, CancellationToken cancellationToken)
    {
        var result = await finder.FindAsync(cancellationToken);
        if (result.IsFailure)
        {
            return result.Error!;
        }

        var images = result.Value!;
        return new UnusedCovers([.. images.Select(i => i.PublicId)], images.Sum(i => i.Bytes));
    }
}
