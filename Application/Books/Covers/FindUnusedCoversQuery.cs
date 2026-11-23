using Application.Abstractions.Messaging;

namespace Application.Books.Covers;

// The images of the cover folders that no book uses, found once every cover was moved.
public record FindUnusedCoversQuery : IQuery<UnusedCovers>;
