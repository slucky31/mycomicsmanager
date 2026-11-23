using Application.Abstractions.Messaging;

namespace Application.Books.Covers;

// Run in the background: copies the cover of every book to its own image.
public record MoveCoversCommand : ICommand<CoverMoveSummary>;
