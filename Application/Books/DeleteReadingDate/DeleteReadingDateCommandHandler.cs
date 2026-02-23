using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.DeleteReadingDate;

public sealed class DeleteReadingDateCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork) : ICommandHandler<DeleteReadingDateCommand>
{
    public async Task<Result> Handle(DeleteReadingDateCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var book = await bookRepository.GetByIdAsync(request.BookId);
        if (book is null)
        {
            return BooksError.NotFound;
        }

        book.RemoveReadingDate(request.ReadingDateId);

        bookRepository.Update(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
