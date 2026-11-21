using Application.Books.AddReadingDate;
using Application.Books.Create;
using Application.Books.Delete;
using Application.Books.DeleteReadingDate;
using Application.Books.GetById;
using Application.Books.IsbnAvailability;
using Application.Books.IsbnOwners;
using Application.Books.List;
using Application.Books.Search;
using Application.Books.Update;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Web.Services;

public class BooksService(
    BookQueryHandlers queries,
    BookCommandHandlers commands,
    ICurrentUserService currentUserService) : IBooksService
{
    public async Task<Result<Book>> GetById(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new GetBookByIdQuery(guidId, UserId: userIdResult.Value);

        return await queries.GetById.Handle(query, CancellationToken.None);
    }

    public async Task<Result<Book>> Create(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new CreateBookCommand(
            request.Series,
            request.Title,
            request.Isbn,
            request.LibraryId,
            userIdResult.Value,
            request.VolumeNumber,
            request.ImageLink,
            request.Rating,
            request.Authors,
            request.Publishers,
            request.PublishDate,
            request.NumberOfPages);

        return await commands.Create.Handle(command, cancellationToken);
    }

    public async Task<Result<Book>> Update(UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request.Id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new UpdateBookCommand(
            guidId,
            request.Series,
            request.Title,
            request.Isbn,
            request.VolumeNumber,
            request.ImageLink,
            userIdResult.Value,
            request.Authors,
            request.Publishers,
            request.PublishDate,
            request.NumberOfPages);

        return await commands.Update.Handle(command, cancellationToken);
    }

    public async Task<Result<bool>> IsIsbnAvailable(string bookId, string isbn, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(bookId, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await queries.IsIsbnAvailable.Handle(new IsIsbnAvailableQuery(guidId, isbn, userIdResult.Value), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<IsbnOwnerDto>>> GetIsbnCandidateOwners(Guid bookId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await queries.GetIsbnCandidateOwners.Handle(new GetIsbnCandidateOwnersQuery(bookId, userIdResult.Value), cancellationToken);
    }

    public async Task<Result<ReadingDate>> AddReadingDate(string bookId, int rating, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(bookId, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new AddReadingDateCommand(guidId, rating, UserId: userIdResult.Value);
        return await commands.AddReadingDate.Handle(command, cancellationToken);
    }

    public async Task<Result> DeleteReadingDate(string bookId, string readingDateId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(bookId, out var bookGuidId) || !Guid.TryParse(readingDateId, out var readingDateGuidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new DeleteReadingDateCommand(bookGuidId, readingDateGuidId, UserId: userIdResult.Value);
        return await commands.DeleteReadingDate.Handle(command, cancellationToken);
    }

    public async Task<Result<List<Book>>> GetAll()
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new GetBooksQuery(userIdResult.Value);

        return await queries.GetBooks.Handle(query, CancellationToken.None);
    }

    public async Task<Result<List<Book>>> GetByLibrary(Guid libraryId, CancellationToken cancellationToken = default)
    {
        if (libraryId == Guid.Empty)
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new GetBooksQuery(userIdResult.Value, LibraryId: libraryId);

        return await queries.GetBooks.Handle(query, cancellationToken);
    }

    public async Task<Result<IPagedList<BookSummaryDto>>> GetPagedByLibrary(Guid libraryId, int page, int pageSize, BookSortOrder sortOrder, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        if (libraryId == Guid.Empty)
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new GetPagedBooksQuery(userIdResult.Value, libraryId, page, pageSize, sortOrder, searchTerm);

        return await queries.GetPagedBooks.Handle(query, cancellationToken);
    }

    public async Task<Result<BookSearchResult>> Search(string searchTerm, int bookLimit, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await queries.Search.Handle(new SearchBooksQuery(userIdResult.Value, searchTerm, bookLimit), cancellationToken);
    }

    public async Task<Result> Delete(string? id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new DeleteBookCommand(guidId, UserId: userIdResult.Value);

        return await commands.Delete.Handle(command, cancellationToken);
    }

    public async Task<Result<DeleteBooksResult>> Delete(IReadOnlyList<Guid> bookIds, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await commands.DeleteBooks.Handle(new DeleteBooksCommand(bookIds, userIdResult.Value), cancellationToken);
    }
}
