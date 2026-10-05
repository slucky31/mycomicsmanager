using Application.Abstractions.Messaging;

namespace Application.Books.MoveTargets;

public record GetBookMoveTargetsQuery(Guid BookId, Guid UserId) : IQuery<BookMoveTargets>;
