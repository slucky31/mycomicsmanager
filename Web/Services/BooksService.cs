using Application.Abstractions.Messaging;
using Application.Books.Create;
using Application.Books.Delete;
using Application.Books.GetById;
using Application.Books.List;
using Application.Books.Update;
using Domain.Books;
using Domain.Primitives;

namespace Web.Services;

public class BooksService(
    IQueryHandler<GetBookByIdQuery, Book> getBookByIdHandler,
    IQueryHandler<GetBooksQuery, List<Book>> getBooksHandler,
    ICommandHandler<CreateBookCommand, Book> createBookHandler,
    ICommandHandler<UpdateBookCommand, Book> updateBookHandler,
    ICommandHandler<DeleteBookCommand> deleteBookHandler) : IBooksService
{
    public async Task<Result<Book>> GetById(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var query = new GetBookByIdQuery(guidId);

        return await getBookByIdHandler.Handle(query, CancellationToken.None);
    }

    public async Task<Result<Book>> Create(string series, string title, string isbn)
    {
        return await Create(series, title, isbn, 1, "", 0);
    }

    public async Task<Result<Book>> Create(string series, string title, string isbn, int volumeNumber)
    {
        return await Create(series, title, isbn, volumeNumber, "", 0);
    }

    public async Task<Result<Book>> Create(string series, string title, string isbn, int volumeNumber, string imageLink, int rating, CancellationToken cancellationToken = default)
    {
        return await Create(series, title, isbn, volumeNumber, imageLink, rating, "", "", null, null, cancellationToken);
    }

    public async Task<Result<Book>> Create(string series, string title, string isbn, int volumeNumber, string imageLink, int rating,
        string authors, string publishers, DateOnly? publishDate, int? numberOfPages, CancellationToken cancellationToken = default)
    {
        var command = new CreateBookCommand(series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);

        return await createBookHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<Book>> Update(string? id, string series, string title, string isbn, int volumeNumber, string imageLink, int rating, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var query = new GetBookByIdQuery(guidId);
        var existingBookResult = await getBookByIdHandler.Handle(query, cancellationToken);
        if (existingBookResult.IsFailure)
        {
            return existingBookResult.Error!;
        }

        var existingBook = existingBookResult.Value!;
        return await Update(id, series, title, isbn, volumeNumber, imageLink, rating,
            existingBook.Authors, existingBook.Publishers, existingBook.PublishDate, existingBook.NumberOfPages, cancellationToken);
    }

    public async Task<Result<Book>> Update(string? id, string series, string title, string isbn, int volumeNumber, string imageLink, int rating,
        string authors, string publishers, DateOnly? publishDate, int? numberOfPages, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var command = new UpdateBookCommand(guidId, series, title, isbn, volumeNumber, imageLink, rating, authors, publishers, publishDate, numberOfPages);

        return await updateBookHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<List<Book>>> GetAll()
    {
        var query = new GetBooksQuery();

        return await getBooksHandler.Handle(query, CancellationToken.None);
    }

    public async Task<Result> Delete(string? id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var command = new DeleteBookCommand(guidId);

        return await deleteBookHandler.Handle(command, cancellationToken);
    }
}
