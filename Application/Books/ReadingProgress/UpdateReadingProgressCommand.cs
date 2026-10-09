using Application.Abstractions.Messaging;

namespace Application.Books.ReadingProgress;

public record UpdateReadingProgressCommand(Guid BookId, Guid UserId, int PageIndex) : ICommand;
