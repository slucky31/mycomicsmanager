using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.AddReadingDate;

public record AddReadingDateCommand(Guid BookId, int Rating, Guid UserId) : ICommand<ReadingDate>;
