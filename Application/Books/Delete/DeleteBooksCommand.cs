using Application.Abstractions.Messaging;
using Domain.Primitives;

namespace Application.Books.Delete;

// Several books of a library deleted at once (#1194).
public record DeleteBooksCommand(IReadOnlyList<Guid> BookIds, Guid UserId) : ICommand<DeleteBooksResult>;

// How many books were deleted; the first error says why the others were not.
public sealed record DeleteBooksResult(int Deleted, int Failed, TError? FirstError = null);
