using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Primitives;

namespace Application.Books.AddReadingDate;

public sealed class AddReadingDateCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork) : ICommandHandler<AddReadingDateCommand, ReadingDate>
{
    public async Task<Result<ReadingDate>> Handle(AddReadingDateCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var book = await bookRepository.GetByIdAsync(request.BookId);
        if (book is null)
        {
            return BooksError.NotFound;
        }

        var readingDate = book.AddReadingDate(DateTime.UtcNow, request.Rating);

        bookRepository.AddReadingDate(readingDate);
        bookRepository.Update(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return readingDate;
    }
}
