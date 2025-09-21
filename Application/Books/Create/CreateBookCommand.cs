using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.Create;

public record CreateBookCommand(
    string Series, 
    string Title, 
    string ISBN, 
    int VolumeNumber = 1, 
    string ImageLink = ""
) : ICommand<Book>;