using Application.Abstractions.Messaging;
using Domain.Books;

namespace Application.Books.Update;

public record UpdateBookCommand(
    Guid Id,
    string Series, 
    string Title, 
    string ISBN, 
    int VolumeNumber, 
    string ImageLink
) : ICommand<Book>;