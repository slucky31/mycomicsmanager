using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.Move;

// Moves a book to another library of the same type. A digital book's file follows it into the folder of the
// target library; every check runs before the file is moved, and the file goes back if the save fails.
public sealed class MoveBookCommandHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository,
    ILibraryLocalStorage libraryLocalStorage,
    IUnitOfWork unitOfWork,
    ILogger<MoveBookCommandHandler> logger) : ICommandHandler<MoveBookCommand, Book>
{
    public async Task<Result<Book>> Handle(MoveBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        if (request.BookId == Guid.Empty || request.TargetLibraryId == Guid.Empty || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var book = await bookRepository.GetByIdAsync(request.BookId);
        var source = book is null ? null : await libraryRepository.GetByIdAsync(book.LibraryId);
        if (book is null || source is null || source.UserId != request.UserId)
        {
            return BooksError.NotFound;
        }

        var target = await libraryRepository.GetByIdAsync(request.TargetLibraryId);
        if (target is null || target.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        var check = CheckMove(book, target);
        if (check.IsFailure)
        {
            return check.Error!;
        }

        return book is DigitalBook digitalBook
            ? await MoveDigitalBookAsync(digitalBook, source, target, cancellationToken)
            : await MovePhysicalBookAsync((PhysicalBook)book, target, cancellationToken);
    }

    private static Result CheckMove(Book book, Library target)
    {
        var expectedType = book is DigitalBook ? LibraryBookType.Digital : LibraryBookType.Physical;
        return target.BookType != expectedType ? LibrariesError.BookTypeMismatch : book.CanMoveToLibrary(target.Id);
    }

    private async Task<Result<Book>> MovePhysicalBookAsync(PhysicalBook book, Library target, CancellationToken cancellationToken)
    {
        var moved = book.MoveToLibrary(target.Id);
        if (moved.IsFailure)
        {
            return moved.Error!;
        }

        bookRepository.Update(book);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error! : book;
    }

    private async Task<Result<Book>> MoveDigitalBookAsync(DigitalBook book, Library source, Library target, CancellationToken cancellationToken)
    {
        var fileMove = libraryLocalStorage.MoveFile(book.FilePath, target.RelativePath);
        if (fileMove.IsFailure)
        {
            return fileMove.Error!;
        }

        var moved = book.MoveToLibrary(target.Id, fileMove.Value!);
        if (moved.IsFailure)
        {
            RestoreFile(fileMove.Value!, source);
            return moved.Error!;
        }

        bookRepository.Update(book);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            RestoreFile(fileMove.Value!, source);
            return saved.Error!;
        }

        logger.LogInformation("Book {BookId} moved from library {SourceLibraryId} to {TargetLibraryId}", book.Id, source.Id, target.Id);
        return book;
    }

    // Compensation: the database still points to the source library, so the file goes back there.
    private void RestoreFile(string movedFilePath, Library source)
    {
        var restored = libraryLocalStorage.MoveFile(movedFilePath, source.RelativePath);
        if (restored.IsFailure)
        {
            logger.LogError("Book file {FilePath} could not be moved back to library {LibraryId}: {Error}",
                movedFilePath, source.Id, restored.Error!.Description);
        }
    }
}
