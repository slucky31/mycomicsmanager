using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.Update;

public record UpdateBookCommand(
    Guid Id,
    string Serie,
    string Title,
    string ISBN,
    int VolumeNumber,
    string ImageLink,
    int Rating,
    string Authors = "",
    string Publishers = "",
    DateOnly? PublishDate = null,
    int? NumberOfPages = null
) : ICommand<Book>;
