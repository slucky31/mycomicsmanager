using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books;

/// <summary>
/// Saves a book. When the metadata of a digital book changed, they are first written into the ComicInfo.xml of its
/// archive; if the save then fails, the previous ComicInfo.xml is written back so that the file and the database agree.
/// </summary>
public sealed class BookSaver(
    IBookRepository bookRepository,
    IUnitOfWork unitOfWork,
    IComicInfoArchiveWriter comicInfoWriter,
    ILogger<BookSaver> logger)
{
    /// <param name="book">The book, once every validation of its changes has passed.</param>
    /// <param name="previousComicInfo">The metadata of the book before its changes, read with <see cref="BookComicInfo.From"/>.</param>
    /// <param name="ct">Cancels the write of the ComicInfo.xml and the save, not the restore of the previous ComicInfo.xml.</param>
    public async Task<Result> SaveAsync(Book book, ComicInfoData previousComicInfo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(book);

        var comicInfo = BookComicInfo.From(book);
        var archive = book is DigitalBook digitalBook && comicInfo != previousComicInfo ? digitalBook.FilePath : null;
        if (archive is not null)
        {
            var writeResult = await comicInfoWriter.WriteAsync(archive, comicInfo, ct);
            if (writeResult.IsFailure)
            {
                return writeResult.Error!;
            }
        }

        bookRepository.Update(book);
        var saveResult = await unitOfWork.SaveChangesAsync(ct);
        if (saveResult.IsSuccess)
        {
            return Result.Success();
        }

        if (archive is not null)
        {
            var restoreResult = await comicInfoWriter.WriteAsync(archive, previousComicInfo, CancellationToken.None);
            if (restoreResult.IsFailure)
            {
                logger.LogError("Book {BookId} not saved and its ComicInfo.xml not restored: {Error}",
                    book.Id, restoreResult.Error?.Description);
            }
        }

        return saveResult.Error!;
    }
}
