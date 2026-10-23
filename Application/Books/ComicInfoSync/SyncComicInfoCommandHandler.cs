using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.ComicInfoSync;

/// <summary>
/// Writes the metadata of every digital book of a user into the ComicInfo.xml of its archive, as saving the book does:
/// it brings back in line the archives of the books saved before the save kept them in sync.
/// </summary>
public sealed class SyncComicInfoCommandHandler(
    IBookRepository bookRepository,
    IComicInfoArchiveWriter comicInfoWriter,
    ILogger<SyncComicInfoCommandHandler> logger) : ICommandHandler<SyncComicInfoCommand, ComicInfoSyncSummary>
{
    public async Task<Result<ComicInfoSyncSummary>> Handle(SyncComicInfoCommand request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var books = await bookRepository.ListByUserIdAsync(request.UserId, cancellationToken);
        int updated = 0, failed = 0;
        foreach (var book in books.OfType<DigitalBook>())
        {
            // A book that cannot be written is reported and the others go on.
            var result = await comicInfoWriter.WriteAsync(book.FilePath, BookComicInfo.From(book), cancellationToken);
            if (result.IsSuccess)
            {
                updated++;
            }
            else
            {
                failed++;
                logger.LogWarning("ComicInfo.xml of book {BookId} not written: {Error}", book.Id, result.Error?.Description);
            }
        }

        return new ComicInfoSyncSummary(updated, failed);
    }
}
