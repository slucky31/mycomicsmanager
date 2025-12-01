using Application.Abstractions.Messaging;
using Application.Books.Helper;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.Create;

public sealed class CreateBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork) : ICommandHandler<CreateBookCommand, Book>
{
    public async Task<Result<Book>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        // Check if parameters are not null or empty
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.ISBN))
        {
            return BooksError.BadRequest;
        }

        // Validate ISBN format (basic check for 10 or 13 digits)
        if (!IsbnValidator.IsValidISBN(request.ISBN))
        {
            return BooksError.InvalidISBN;
        }

        // Check if a book with the same ISBN doesn't already exist
        var existingBook = await bookRepository.GetByIsbnAsync(request.ISBN, cancellationToken);
        if (existingBook is not null)
        {
            return BooksError.Duplicate;
        }

        // Create Book
        var book = Book.Create(request.Serie, request.Title, request.ISBN, request.VolumeNumber, request.ImageLink);

        bookRepository.Add(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return book;
    }
    
}
