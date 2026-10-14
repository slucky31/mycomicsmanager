namespace Web.Models;

// The steps of adding a book, each one keeping the library the book is added to (ADR-0021).
public static class AddBookLinks
{
    public static string Scan(Guid? libraryId) => WithLibrary("/books/add/scan", libraryId);

    public static string TypeIsbn(Guid? libraryId) => WithLibrary("/books/add/manual", libraryId);

    public static string EnterDetails(Guid? libraryId) => WithLibrary("/books/add/form", libraryId);

    // The book found for this ISBN, or the form when no catalogue knows it.
    public static string LookUp(string isbn, Guid? libraryId) =>
        WithLibrary($"/books/add/form?isbn={Uri.EscapeDataString(isbn)}", libraryId);

    // Where closing or finishing the task goes: the library, or the list of the libraries.
    public static string Exit(Guid? libraryId) => libraryId is { } id ? $"/libraries/{id}" : "/libraries/list";

    public static Guid? ParseLibraryId(string? value) => Guid.TryParse(value, out var id) ? id : null;

    private static string WithLibrary(string path, Guid? libraryId)
    {
        if (libraryId is not { } id)
        {
            return path;
        }

        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}libraryId={id}";
    }
}
