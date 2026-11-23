using Application.Abstractions.Messaging;

namespace Application.Books.Covers;

// The books of every user whose cover is still an image shared under its ISBN or in digital-covers (#1225).
public record CountCoversToMoveQuery : IQuery<int>;
