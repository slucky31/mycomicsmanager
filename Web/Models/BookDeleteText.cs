using Application.Books.Delete;
using MudBlazor;

namespace Web.Models;

// What the deletion of several books of a library says (#1194).
public static class BookDeleteText
{
    public static string Title(int count) => count == 1 ? "Delete 1 book" : $"Delete {count} books";

    // The files of a digital library are deleted with the books.
    public static string Confirmation(int count, bool isDigital)
    {
        if (count == 1)
        {
            return $"This book will be deleted with its readings{(isDigital ? " and its file" : string.Empty)}. This cannot be undone.";
        }

        return $"These {count} books will be deleted with their readings{(isDigital ? " and their files" : string.Empty)}. This cannot be undone.";
    }

    // "3 books deleted", "2 of 3 books deleted. The book was not found", "No book was deleted. …".
    public static string Outcome(DeleteBooksResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return SeveralBooksText.Outcome(result.Deleted, result.Failed, result.FirstError, "deleted");
    }

    public static Severity Severity(DeleteBooksResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return SeveralBooksText.Severity(result.Deleted, result.Failed);
    }
}
