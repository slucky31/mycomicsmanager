using Application.Abstractions.Messaging;

namespace Application.Books.MoveTargets;

// The books are in the same library: one book from its page, several chosen in their library (#1150).
public record GetBookMoveTargetsQuery(IReadOnlyList<Guid> BookIds, Guid UserId) : IQuery<BookMoveTargets>
{
    public GetBookMoveTargetsQuery(Guid bookId, Guid userId) : this([bookId], userId)
    {
    }
}
