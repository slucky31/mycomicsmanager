using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.Create;

public record CreateBookCommand(
    string Serie,
    string Title,
    string ISBN,
    int VolumeNumber = 1,
    string ImageLink = "",
    int Rating = 0,
    string Authors = "",
    string Publishers = "",
    DateOnly? PublishDate = null,
    int? NumberOfPages = null
) : ICommand<Book>;
