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

    public async Task<Result<Book>> Create(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateBookCommand(
            request.Series,
            request.Title,
            request.Isbn,
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

        var command = new UpdateBookCommand(
            guidId,
            request.Series,
            request.Title,
            request.Isbn,
            request.VolumeNumber,
            request.ImageLink,
            request.Rating,
            request.Authors,
            request.Publishers,
            request.PublishDate,
            request.NumberOfPages);

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
