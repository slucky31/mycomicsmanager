using Application.Abstractions.Messaging;
using Domain.Primitives;

namespace Application.Books.Move;

// Several books of a library moved at once (#1150).
public record MoveBooksCommand(IReadOnlyList<Guid> BookIds, Guid TargetLibraryId, Guid UserId) : ICommand<MoveBooksResult>;

// How many books were moved; the first error says why the others were not.
public sealed record MoveBooksResult(int Moved, int Failed, TError? FirstError = null);
