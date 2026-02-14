using Application.Abstractions.Messaging;
using Application.Helpers;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.Update;

public sealed class UpdateBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork) : ICommandHandler<UpdateBookCommand, Book>
{
    public async Task<Result<Book>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        // Check if parameters are not null or empty
        if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.ISBN) || string.IsNullOrEmpty(request.Serie))
        {
            return BooksError.BadRequest;
        }

        // Validate ISBN format
        if (!IsbnHelper.IsValidISBN(request.ISBN))
        {
            return BooksError.InvalidISBN;
        }
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(request.ISBN);

        // Check if another book with the same ISBN exists (excluding current book)
        var existingBook = await bookRepository.GetByIsbnAsync(normalizedIsbn, cancellationToken);
        if (existingBook is not null && existingBook.Id != request.Id)
        {
            return BooksError.Duplicate;
        }

        // Get the existing book
        var book = await bookRepository.GetByIdAsync(request.Id);
        if (book == null)
        {
            return BooksError.NotFound;
        }

        // Update the book
        book.Update(request.Serie, request.Title, normalizedIsbn, request.VolumeNumber, request.ImageLink, request.Rating,
            request.Authors, request.Publishers, request.PublishDate, request.NumberOfPages);

        bookRepository.Update(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return book;
    }

}
