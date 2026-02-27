using Application.Abstractions.Messaging;
using Application.Books.AddReadingDate;
using Application.Books.Create;
using Application.Books.Delete;
using Application.Books.DeleteReadingDate;
using Application.Books.GetById;
using Application.Books.List;
using Application.Books.Update;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;

namespace Web.Services;

public class BooksService(
    IQueryHandler<GetBookByIdQuery, Book> getBookByIdHandler,
    IQueryHandler<GetBooksQuery, List<Book>> getBooksHandler,
    ICommandHandler<CreateBookCommand, Book> createBookHandler,
    ICommandHandler<UpdateBookCommand, Book> updateBookHandler,
    ICommandHandler<DeleteBookCommand> deleteBookHandler,
    ICommandHandler<AddReadingDateCommand, ReadingDate> addReadingDateHandler,
    ICommandHandler<DeleteReadingDateCommand> deleteReadingDateHandler,
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
            return userIdResult.Error;
        }

        var query = new GetBookByIdQuery(guidId, UserId: userIdResult.Value);

        return await getBookByIdHandler.Handle(query, CancellationToken.None);
    }

    public async Task<Result<Book>> Create(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error;
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

        return await createBookHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<Book>> Update(UpdateBookRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request.Id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error;
        }

        var command = new UpdateBookCommand(
            guidId,
            request.Series,
            request.Title,
            request.Isbn,
            request.VolumeNumber,
            request.ImageLink,
            request.Authors,
            request.Publishers,
            request.PublishDate,
            request.NumberOfPages,
            UserId: userIdResult.Value);

        return await updateBookHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<ReadingDate>> AddReadingDate(string bookId, int rating, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(bookId, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error;
        }

        var command = new AddReadingDateCommand(guidId, rating, UserId: userIdResult.Value);
        return await addReadingDateHandler.Handle(command, cancellationToken);
    }

    public async Task<Result> DeleteReadingDate(string bookId, string readingDateId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(bookId, out var bookGuidId) || !Guid.TryParse(readingDateId, out var readingDateGuidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error;
        }

        var command = new DeleteReadingDateCommand(bookGuidId, readingDateGuidId, UserId: userIdResult.Value);
        return await deleteReadingDateHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<List<Book>>> GetAll()
    {
        var query = new GetBooksQuery();

        return await getBooksHandler.Handle(query, CancellationToken.None);
    }

    public async Task<Result<List<Book>>> GetByLibrary(Guid libraryId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error;
        }

        var query = new GetBooksQuery(LibraryId: libraryId, UserId: userIdResult.Value);

        return await getBooksHandler.Handle(query, cancellationToken);
    }

    public async Task<Result> Delete(string? id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error;
        }

        var command = new DeleteBookCommand(guidId, UserId: userIdResult.Value);

        return await deleteBookHandler.Handle(command, cancellationToken);
    }
}
