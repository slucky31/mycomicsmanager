using Application.Abstractions.Messaging;

namespace Application.Books.IsbnOwners;

// The book of the owner, of the same type as the library (physical or digital), that already holds the ISBN
// typed in the book form (#1343): empty when the ISBN is free, or held by the edited book itself.
public record GetIsbnOwnerQuery(Guid LibraryId, Guid BookId, string Isbn, Guid UserId) : IQuery<IReadOnlyList<IsbnOwnerDto>>;
