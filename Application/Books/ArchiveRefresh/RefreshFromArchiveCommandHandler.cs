using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.Books.ArchiveRefresh;

/// <summary>
/// Reads again the number of pages and the cover of every digital book of a user from its archive (#1193):
/// it brings back the values replaced by the web sources before they were kept from the archive.
/// </summary>
public sealed class RefreshFromArchiveCommandHandler(
    IBookRepository bookRepository,
    IComicPageReader pageReader,
    ICloudinaryService cloudinary,
    BookSaver bookSaver,
    ILogger<RefreshFromArchiveCommandHandler> logger) : ICommandHandler<RefreshFromArchiveCommand, ArchiveRefreshSummary>
{
    private static readonly Dictionary<string, string> s_extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/webp"] = ".webp",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
    };

    public async Task<Result<ArchiveRefreshSummary>> Handle(RefreshFromArchiveCommand request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var books = await bookRepository.ListByUserIdAsync(request.UserId, cancellationToken);
        var digitalBooks = books.OfType<DigitalBook>().ToList();
        logger.LogInformation("Reading the cover and pages of {Count} digital book(s) of user {UserId} from their archive",
            digitalBooks.Count, request.UserId);

        int updated = 0, unchanged = 0, failed = 0;
        foreach (var book in digitalBooks)
        {
            // A book that cannot be read is reported and the others go on.
            var result = await RefreshAsync(book, cancellationToken);
            if (result.IsFailure)
            {
                failed++;
                logger.LogWarning("Cover and pages of book {BookId} not read from {Path}: {Error}",
                    book.Id, book.FilePath, result.Error?.Description);
            }
            else if (result.Value)
            {
                updated++;
            }
            else
            {
                unchanged++;
            }
        }

        return new ArchiveRefreshSummary(updated, unchanged, failed);
    }

    // True when the book changed.
    private async Task<Result<bool>> RefreshAsync(DigitalBook book, CancellationToken ct)
    {
        var countResult = await pageReader.CountPagesAsync(book.FilePath, ct);
        if (countResult.IsFailure)
        {
            return countResult.Error!;
        }

        if (countResult.Value <= 0)
        {
            return BooksError.PageNotFound;
        }

        var cover = DigitalCovers.IsFromArchive(book.ImageLink) ? null : await UploadCoverAsync(book, ct);
        if (book.NumberOfPages == countResult.Value && cover is null)
        {
            return false;
        }

        var previousComicInfo = BookComicInfo.From(book);
        var refreshResult = book.RefreshFromArchive(countResult.Value, cover);
        if (refreshResult.IsFailure)
        {
            return refreshResult.Error!;
        }

        var saveResult = await bookSaver.SaveAsync(book, previousComicInfo, ct);
        if (saveResult.IsFailure)
        {
            return saveResult.Error!;
        }

        return true;
    }

    // The first page, uploaded as the import does; null when it cannot be, the current cover is then kept.
    private async Task<string?> UploadCoverAsync(DigitalBook book, CancellationToken ct)
    {
        var pageResult = await pageReader.ReadPageAsync(book.FilePath, 0, ct);
        if (pageResult.IsFailure)
        {
            logger.LogWarning("First page of book {BookId} not read: {Error}", book.Id, pageResult.Error?.Description);
            return null;
        }

        var page = pageResult.Value!;
        await using var stream = new MemoryStream(page.Content.ToArray(), writable: false);
        var fileName = "cover" + s_extensions.GetValueOrDefault(page.ContentType, ".jpg");
        var uploadResult = await cloudinary.UploadImageFromStreamAsync(
            stream, fileName, DigitalCovers.Folder, DigitalCovers.PublicId(book.ISBN, book.Id), ct);

        if (uploadResult.Success && uploadResult.Url is not null)
        {
            return uploadResult.Url.ToString();
        }

        logger.LogWarning("Cover of book {BookId} not uploaded: {Error}", book.Id, uploadResult.Error);
        return null;
    }
}
