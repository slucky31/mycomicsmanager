using Application.Abstractions.Messaging;
using Application.Helpers;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.Create;

public sealed class CreateBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork, IRepository<Library, Guid> libraryRepository) : ICommandHandler<CreateBookCommand, Book>
{
    public async Task<Result<Book>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        // Check if parameters are not null or empty
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.ISBN) || string.IsNullOrWhiteSpace(request.Serie)
            || request.LibraryId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        // Validate ISBN format (basic check for 10 or 13 digits)
        if (!IsbnHelper.IsValidISBN(request.ISBN))
        {
            return BooksError.InvalidISBN;
        }

        // Validate rating range: 0 when the book was not rated (not read yet)
        if (request.Rating is < 0 or > 5)
        {
            return BooksError.InvalidRating;
        }

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(request.ISBN);

        // Fetch the library
        var library = await libraryRepository.GetByIdAsync(request.LibraryId);
        if (library is null)
        {
            return LibrariesError.NotFound;
        }

        // Verify ownership
        if (library.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        // The same ISBN can be owned once in Physical and once in Digital, not twice in the same type
        var existingBook = await bookRepository.GetByIsbnAsync(normalizedIsbn, request.LibraryId, cancellationToken);
        if (existingBook is not null)
        {
            return BooksError.Duplicate;
        }

        // Create Book
        var metadata = new BookMetadata(request.Serie, request.Title, normalizedIsbn,
            request.VolumeNumber, request.ImageLink, request.Authors, request.Publishers,
            request.PublishDate, request.NumberOfPages);
        var createResult = PhysicalBook.Create(metadata, request.LibraryId);
        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        var book = createResult.Value!;

        // Validate book type compatibility with library
        var canContainResult = library.CanContain(book);
        if (canContainResult.IsFailure)
        {
            return canContainResult.Error!;
        }

        // A rated book was read: its reading is dated today
        if (request.Rating > 0)
        {
            book.AddReadingDate(DateTime.UtcNow, request.Rating);
        }

        bookRepository.Add(book);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error!;
        }

        return book;
    }
}
