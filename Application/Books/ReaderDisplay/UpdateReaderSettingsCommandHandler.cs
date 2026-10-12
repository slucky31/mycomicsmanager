using Application.Abstractions.Messaging;
using Application.Books.Read;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.ReaderDisplay;

public sealed class UpdateReaderSettingsCommandHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateReaderSettingsCommand>
{
    public async Task<Result> Handle(UpdateReaderSettingsCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.BookId == Guid.Empty || command.UserId == Guid.Empty ||
            command.Settings is null || !command.Settings.IsValid())
        {
            return BooksError.BadRequest;
        }

        var bookResult = await OwnedDigitalBook.GetAsync(bookRepository, libraryRepository, command.BookId, command.UserId);
        if (bookResult.IsFailure)
        {
            return bookResult.Error!;
        }

        var book = bookResult.Value!;
        if (book.ReaderSettings == command.Settings)
        {
            return Result.Success();
        }

        var updateResult = book.UpdateReaderSettings(command.Settings);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        bookRepository.Update(book);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error! : Result.Success();
    }
}
