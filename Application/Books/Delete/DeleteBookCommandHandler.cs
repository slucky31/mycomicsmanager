using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.Delete;

public sealed class DeleteBookCommandHandler(
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork,
    IRepository<Library, Guid> libraryRepository) : ICommandHandler<DeleteBookCommand>
{
    public async Task<Result> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

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

        bookRepository.Remove(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
