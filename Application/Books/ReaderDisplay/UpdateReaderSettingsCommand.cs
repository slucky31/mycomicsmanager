using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.ReaderDisplay;

public record UpdateReaderSettingsCommand(Guid BookId, Guid UserId, ReaderSettings Settings) : ICommand;
