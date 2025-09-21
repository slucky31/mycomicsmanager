using Application.Abstractions.Messaging;
using Application.Books.Create;
using Application.Books.Delete;
using Application.Books.GetById;
using Application.Books.List;
using Application.Books.Update;
using Domain.Books;
using Domain.Primitives;

namespace Web.Services;

public class BooksService : IBooksService
{
    private readonly ICommandHandler<CreateBookCommand, Book> handler_CreateBookCommand;
    private readonly ICommandHandler<UpdateBookCommand, Book> handler_UpdateBookCommand;
    private readonly ICommandHandler<DeleteBookCommand> handler_DeleteBookCommand;

    private readonly IQueryHandler<GetBookQuery, Book> handler_GetBookQuery;
    private readonly IQueryHandler<GetBooksQuery, List<Book>> handler_GetBooksQuery;

    public BooksService(IQueryHandler<GetBookQuery, Book> handler_GetBookQuery,
                        IQueryHandler<GetBooksQuery, List<Book>> handler_GetBooksQuery,
                        ICommandHandler<CreateBookCommand, Book> handler_CreateBookCommand,
                        ICommandHandler<UpdateBookCommand, Book> handler_UpdateBookCommand,
                        ICommandHandler<DeleteBookCommand> handler_DeleteBookCommand)
    {
        this.handler_GetBookQuery = handler_GetBookQuery;
        this.handler_GetBooksQuery = handler_GetBooksQuery;
        this.handler_CreateBookCommand = handler_CreateBookCommand;
        this.handler_UpdateBookCommand = handler_UpdateBookCommand;
        this.handler_DeleteBookCommand = handler_DeleteBookCommand;
    }

    public async Task<Result<Book>> GetById(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var query = new GetBookQuery(guidId);

        return await handler_GetBookQuery.Handle(query, CancellationToken.None);
    }

    public async Task<Result<Book>> Create(string series, string title, string isbn, int volumeNumber = 1, string imageLink = "")
    {
        var command = new CreateBookCommand(series, title, isbn, volumeNumber, imageLink);

        return await handler_CreateBookCommand.Handle(command, CancellationToken.None);
    }

    public async Task<Result<Book>> Update(string? id, string series, string title, string isbn, int volumeNumber, string imageLink)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var command = new UpdateBookCommand(guidId, series, title, isbn, volumeNumber, imageLink);

        return await handler_UpdateBookCommand.Handle(command, CancellationToken.None);
    }

    public async Task<Result<List<Book>>> GetAll()
    {
        var query = new GetBooksQuery();

        return await handler_GetBooksQuery.Handle(query, CancellationToken.None);
    }

    public async Task<Result> Delete(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BooksError.ValidationError;
        }

        var command = new DeleteBookCommand(guidId);

        return await handler_DeleteBookCommand.Handle(command, CancellationToken.None);
    }
}