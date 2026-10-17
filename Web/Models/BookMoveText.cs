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

        var total = result.Moved + result.Failed;
        if (result.Failed == 0)
        {
            return total == 1 ? "1 book moved" : $"{total} books moved";
        }

        var reason = result.FirstError?.Description;
        var outcome = result.Moved == 0 ? "No book was moved" : $"{result.Moved} of {total} books moved";
        return string.IsNullOrEmpty(reason) ? outcome : $"{outcome}. {reason}";
    }

    public static Severity Severity(MoveBooksResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Failed == 0)
        {
            return MudBlazor.Severity.Success;
        }

        return result.Moved == 0 ? MudBlazor.Severity.Error : MudBlazor.Severity.Warning;
    }
}
