using Application.Abstractions.Messaging;

namespace Application.Books.IsbnOwners;

// The other books of the owner, of the same type, that already hold an ISBN read on the pages of the book (#1276).
public record GetIsbnCandidateOwnersQuery(Guid BookId, Guid UserId) : IQuery<IReadOnlyList<IsbnOwnerDto>>;
