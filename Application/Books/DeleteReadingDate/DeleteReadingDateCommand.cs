using Application.Abstractions.Messaging;

namespace Application.Books.DeleteReadingDate;

public record DeleteReadingDateCommand(Guid BookId, Guid ReadingDateId) : ICommand;
