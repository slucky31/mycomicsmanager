using Application.Abstractions.Messaging;
using Application.Books.Read;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.ReadingProgress;

public sealed class UpdateReadingProgressCommandHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateReadingProgressCommand>
{
    public async Task<Result> Handle(UpdateReadingProgressCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.BookId == Guid.Empty || command.UserId == Guid.Empty || command.PageIndex < 0)
        {
            return BooksError.BadRequest;
        }

        var bookResult = await OwnedDigitalBook.GetAsync(bookRepository, libraryRepository, command.BookId, command.UserId);
        if (bookResult.IsFailure)
        {
            return bookResult.Error!;
        }

        var book = bookResult.Value!;
        if (book.LastReadPage == command.PageIndex)
        {
            return Result.Success();
        }

        var updateResult = book.UpdateReadingProgress(command.PageIndex);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        bookRepository.Update(book);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error! : Result.Success();
    }
}
