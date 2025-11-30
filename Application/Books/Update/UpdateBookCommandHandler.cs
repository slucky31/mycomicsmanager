using Application.Abstractions.Messaging;
using Application.Books.Helper;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.Update;

public sealed class UpdateBookCommandHandler(IRepository<Book, Guid> bookRepository, IUnitOfWork unitOfWork) : ICommandHandler<UpdateBookCommand, Book>
{
    public async Task<Result<Book>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        // Check if parameters are not null or empty
        if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.ISBN))
        {
            return BooksError.BadRequest;
        }

        // Validate ISBN format
        if (!IsbnValidator.IsValidISBN(request.ISBN))
        {
            return BooksError.InvalidISBN;
        }

        // Get the existing book
        var book = await bookRepository.GetByIdAsync(request.Id);
        if (book == null)
        {
            return BooksError.NotFound;
        }

        // Check if another book with the same ISBN exists (excluding current book)
        var existingBooks = await bookRepository.ListAsync();
        if (existingBooks.Exists(b => b.ISBN == request.ISBN && b.Id != request.Id))
        {
            return BooksError.Duplicate;
        }

        // Update the book
        book.Update(request.Serie, request.Title, request.ISBN, request.VolumeNumber, request.ImageLink);

        bookRepository.Update(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return book;
    }
    
}
