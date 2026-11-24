using Application.ComicInfoSearch;
using Application.Interfaces;
using Domain.Books;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Books;

/// <summary>
/// Stores the cover of a book in its own Cloudinary image, &lt;folder&gt;/&lt;book id&gt; (#1225): a cover found on the web,
/// or shared with other books under its ISBN, is copied there. A cover that cannot be copied is kept as it is.
/// </summary>
public sealed class BookCoverStore(
    ICloudinaryService cloudinary,
    IOptions<CloudinarySettings> settings,
    ILogger<BookCoverStore> logger)
{
    public string Folder => settings.Value.Folder;

    // The folders where the unused images are looked for: the one of this environment, and the old digital-covers.
    public IReadOnlyList<string> Folders => string.Equals(Folder, BookCovers.LegacyDigitalFolder, StringComparison.Ordinal)
        ? [Folder]
        : [Folder, BookCovers.LegacyDigitalFolder];

    // True when the cover is a Cloudinary image that is not the book's own: shared under its ISBN, or in digital-covers.
    public bool IsShared(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        return BookCovers.IsCloudinary(book.ImageLink) && !BookCovers.IsStoredFor(book.ImageLink, Folder, book.Id);
    }

    // True when the cover is a web image that is not yet the image of the book.
    public bool NeedsCopy(Book book)
    {
        ArgumentNullException.ThrowIfNull(book);

        return Uri.TryCreate(book.ImageLink, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && !BookCovers.IsStoredFor(book.ImageLink, Folder, book.Id);
    }

    // Copies the cover to the image of the book; true when the book now uses it.
    public async Task<bool> StoreAsync(Book book, CancellationToken ct)
    {
        if (!NeedsCopy(book))
        {
            return false;
        }

        var link = await UploadUrlAsync(book.Id, new Uri(book.ImageLink), ct);
        if (link is null)
        {
            return false;
        }

        book.UseStoredCover(link);
        return true;
    }

    // Copies a web image as the image of the book; null when it cannot be.
    public async Task<string?> UploadUrlAsync(Guid bookId, Uri imageUrl, CancellationToken ct) =>
        Link(bookId, await cloudinary.UploadImageFromUrlAsync(imageUrl, Folder, BookCovers.PublicId(bookId), ct));

    // Uploads a page of the archive as the image of the book; null when it cannot be.
    public async Task<string?> UploadFileAsync(Guid bookId, string filePath, CancellationToken ct) =>
        Link(bookId, await cloudinary.UploadImageFromFileAsync(filePath, Folder, BookCovers.PublicId(bookId), ct));

    public async Task<string?> UploadStreamAsync(Guid bookId, Stream image, string fileName, CancellationToken ct) =>
        Link(bookId, await cloudinary.UploadImageFromStreamAsync(image, fileName, Folder, BookCovers.PublicId(bookId), ct));

    // Deletes the own images of books that were deleted (#1365): an image shared with other books, or found on the web,
    // stays. A failure is only logged, the books are already deleted and the unused images can be cleaned in Settings.
    public async Task DeleteAsync(IEnumerable<Book> books, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(books);

        var publicIds = books
            .Where(book => BookCovers.IsStoredFor(book.ImageLink, Folder, book.Id))
            .Select(book => $"{Folder}/{BookCovers.PublicId(book.Id)}")
            .ToList();
        if (publicIds.Count == 0)
        {
            return;
        }

        var result = await cloudinary.DeleteImagesAsync(publicIds, ct);
        if (result.IsFailure)
        {
            logger.LogWarning("Covers of {Count} deleted book(s) not deleted from Cloudinary: {Error}",
                publicIds.Count, result.Error?.Description);
        }
    }

    private string? Link(Guid bookId, CloudinaryUploadResult? result)
    {
        if (result is { Success: true, Url: not null })
        {
            return result.Url.ToString();
        }

        logger.LogWarning("Cover of book {BookId} not uploaded: {Error}", bookId, result?.Error);
        return null;
    }
}
