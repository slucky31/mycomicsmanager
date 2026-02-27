using Application.Abstractions.Messaging;
using Application.Helpers;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.Update;

public sealed class UpdateBookCommandHandler(
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork,
    IRepository<Library, Guid> libraryRepository) : ICommandHandler<UpdateBookCommand, Book>
{
    public async Task<Result<Book>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.ISBN) || string.IsNullOrEmpty(request.Serie))
        {
            return BooksError.BadRequest;
        }

        if (!IsbnHelper.IsValidISBN(request.ISBN))
        {
            return BooksError.InvalidISBN;
        }
        var normalizedIsbn = IsbnHelper.NormalizeIsbn(request.ISBN);

        var existingBook = await bookRepository.GetByIsbnAsync(normalizedIsbn, cancellationToken);
        if (existingBook is not null && existingBook.Id != request.Id)
        {
            return BooksError.Duplicate;
        }

        var book = await bookRepository.GetByIdAsync(request.Id);
        if (book == null)
        {
            return BooksError.NotFound;
        }

        var library = await libraryRepository.GetByIdAsync(book.LibraryId);
        if (library is null || library.UserId != request.UserId)
        {
            return BooksError.NotFound;
        }

        book.Update(request.Serie, request.Title, normalizedIsbn, request.VolumeNumber, request.ImageLink,
            request.Authors, request.Publishers, request.PublishDate, request.NumberOfPages);

        bookRepository.Update(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return book;
    }
}
