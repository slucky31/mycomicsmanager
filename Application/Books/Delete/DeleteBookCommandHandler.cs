using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.Delete;

public sealed class DeleteBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork) : ICommandHandler<DeleteBookCommand>
{
    public async Task<Result> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        // Get the existing book
        var book = await bookRepository.GetByIdAsync(request.Id);
        if (book == null)
        {
            return BooksError.NotFound;
        }

        bookRepository.Remove(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
