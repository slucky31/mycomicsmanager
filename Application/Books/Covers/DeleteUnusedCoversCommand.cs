using Application.Abstractions.Messaging;

namespace Application.Books.Covers;

// Deletes these images, if no book uses them when the deletion starts.
public record DeleteUnusedCoversCommand(IReadOnlyList<string> PublicIds) : ICommand<DeletedCovers>;
