using Application.Abstractions.Messaging;
using Application.Helpers;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.Update;

public sealed class UpdateBookCommandHandler(
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork,
    IRepository<Library, Guid> libraryRepository,
    IComicInfoArchiveWriter comicInfoWriter,
    ILogger<UpdateBookCommandHandler> logger) : ICommandHandler<UpdateBookCommand, Book>
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
            ? await bookRepository.GetByIsbnAsync(normalizedIsbn, cancellationToken)
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

        var saveResult = await SaveAsync(book, previousComicInfo, cancellationToken);
        return saveResult.IsFailure ? saveResult.Error! : book;
    }

    // The ComicInfo.xml of a digital book is written once every validation has passed, then the book is saved;
    // if the save fails, the previous ComicInfo.xml is written back so that the file and the database agree.
    private async Task<Result> SaveAsync(Book book, ComicInfoData previousComicInfo, CancellationToken ct)
    {
        var digitalBook = book as DigitalBook;
        if (digitalBook is not null)
        {
            var writeResult = await comicInfoWriter.WriteAsync(digitalBook.FilePath, BookComicInfo.From(book), ct);
            if (writeResult.IsFailure)
            {
                return writeResult.Error!;
            }
        }

        bookRepository.Update(book);
        var saveResult = await unitOfWork.SaveChangesAsync(ct);
        if (saveResult.IsFailure && digitalBook is not null)
        {
            var restoreResult = await comicInfoWriter.WriteAsync(digitalBook.FilePath, previousComicInfo, CancellationToken.None);
            if (restoreResult.IsFailure)
            {
                logger.LogError("Book {BookId} not saved and its ComicInfo.xml not restored: {Error}",
                    book.Id, restoreResult.Error?.Description);
            }
        }

        return saveResult.IsFailure ? saveResult.Error! : Result.Success();
    }
}
