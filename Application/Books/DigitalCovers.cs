namespace Application.Books;

// The cover of a digital book is its first page, uploaded to this Cloudinary folder (#1193).
public static class DigitalCovers
{
    public const string Folder = "digital-covers";

    public static bool IsFromArchive(string? imageLink) =>
        imageLink?.Contains($"/{Folder}/", StringComparison.OrdinalIgnoreCase) == true;

    // The ISBN names the cover, as the archive; a book without one is named by an id.
    public static string PublicId(string? isbn, Guid id) => string.IsNullOrWhiteSpace(isbn) ? $"digital-{id}" : isbn;
}
