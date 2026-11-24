using Domain.Books;
using Domain.Libraries;

namespace Application.Libraries;

internal static class LibraryFolder
{
    /// <summary>
    /// Whether the folder of the library holds a file that none of its books points to: it belongs to another
    /// library sharing the folder (#1369), or to nobody. Deleting or moving the folder would then take it along.
    /// </summary>
    internal static bool HasForeignFiles(ILibraryLocalStorage storage, Library library, IEnumerable<Book> books)
    {
        var bookFiles = books.OfType<DigitalBook>()
            .Where(b => !string.IsNullOrEmpty(b.FilePath))
            .Select(b => Path.GetFullPath(b.FilePath))
            .ToHashSet(StringComparer.Ordinal);

        return storage.ListFiles(library.RelativePath).Any(file => !bookFiles.Contains(Path.GetFullPath(file)));
    }
}
