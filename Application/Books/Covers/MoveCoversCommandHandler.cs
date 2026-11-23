using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.Covers;

public sealed class MoveCoversCommandHandler(
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork,
    BookCoverStore coverStore,
    ILogger<MoveCoversCommandHandler> logger) : ICommandHandler<MoveCoversCommand, CoverMoveSummary>
{
    public async Task<Result<CoverMoveSummary>> Handle(MoveCoversCommand request, CancellationToken cancellationToken)
    {
        var books = (await bookRepository.ListAsync(cancellationToken)).Where(coverStore.NeedsCopy).ToList();
        logger.LogInformation("Copying the cover of {Count} book(s) to their own image", books.Count);

        int moved = 0, failed = 0;
        foreach (var book in books)
        {
            // A cover that cannot be copied is kept, and the other books go on.
            if (await coverStore.StoreAsync(book, cancellationToken) && await SaveAsync(book, cancellationToken))
            {
                moved++;
            }
            else
            {
                failed++;
            }
        }

        return new CoverMoveSummary(moved, failed);
    }

    private async Task<bool> SaveAsync(Book book, CancellationToken ct)
    {
        bookRepository.Update(book);
        var saveResult = await unitOfWork.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            logger.LogWarning("Cover of book {BookId} copied but not saved: {Error}", book.Id, saveResult.Error?.Description);
        }

        return saveResult.IsSuccess;
    }
}
