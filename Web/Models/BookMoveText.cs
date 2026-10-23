using Application.Books.Move;
using MudBlazor;

namespace Web.Models;

// What the move of several books of a library says (#1150).
public static class BookMoveText
{
    public static string Title(int count) => count == 1 ? "Move 1 book" : $"Move {count} books";

    // "3 books moved", "2 of 3 books moved. A file with the same name…", "No book was moved. …".
    public static string Outcome(MoveBooksResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return SeveralBooksText.Outcome(result.Moved, result.Failed, result.FirstError, "moved");
    }

    public static Severity Severity(MoveBooksResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return SeveralBooksText.Severity(result.Moved, result.Failed);
    }
}
