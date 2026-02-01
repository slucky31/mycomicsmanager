using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.Create;

public record CreateBookCommand(
    string Serie,
    string Title,
    string ISBN,
    int VolumeNumber = 1,
    string ImageLink = "",
    int Rating = 0
) : ICommand<Book>;
