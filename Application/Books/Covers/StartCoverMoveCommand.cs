using Application.Abstractions.Messaging;

namespace Application.Books.Covers;

// Returns the number of books whose cover will be copied to their own image in the background.
public record StartCoverMoveCommand : ICommand<int>;
