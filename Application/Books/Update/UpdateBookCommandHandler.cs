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
    IRepository<Library, Guid> libraryRepository,
    BookSaver bookSaver) : ICommandHandler<UpdateBookCommand, Book>
{
    public async Task<Result<Book>> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Serie))
        {
            return BooksError.BadRequest;
        }

        string? normalizedIsbn = null;
        if (!string.IsNullOrWhiteSpace(request.ISBN))
        {
            if (!IsbnHelper.IsValidISBN(request.ISBN))
            {
                return BooksError.InvalidISBN;
            }
            normalizedIsbn = IsbnHelper.NormalizeIsbn(request.ISBN);
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

        var existingBook = normalizedIsbn is not null
            ? await bookRepository.GetByIsbnAsync(normalizedIsbn, book.LibraryId, cancellationToken)
            : null;
        if (existingBook is not null && existingBook.Id != request.Id)
        {
            return BooksError.Duplicate;
        }

        var previousComicInfo = BookComicInfo.From(book);
        var updateResult = book.Update(new BookMetadata(request.Serie, request.Title, normalizedIsbn,
            request.VolumeNumber, request.ImageLink, request.Authors, request.Publishers,
            request.PublishDate, request.NumberOfPages));
        if (updateResult.IsFailure)
        {
            return updateResult.Error!;
        }

        var saveResult = await bookSaver.SaveAsync(book, previousComicInfo, cancellationToken);
        return saveResult.IsFailure ? saveResult.Error! : book;
    }
}
