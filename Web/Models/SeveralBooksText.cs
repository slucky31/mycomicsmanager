using Domain.Primitives;
using MudBlazor;

namespace Web.Models;

// What an action on several books of a library says once done (#1150, #1194), by the verb of the action.
public static class SeveralBooksText
{
    // "3 books moved", "2 of 3 books deleted. The book was not found", "No book was moved. …".
    public static string Outcome(int done, int failed, TError? firstError, string verb)
    {
        var total = done + failed;
        if (failed == 0)
        {
            return total == 1 ? $"1 book {verb}" : $"{total} books {verb}";
        }

        var reason = firstError?.Description;
        var outcome = done == 0 ? $"No book was {verb}" : $"{done} of {total} books {verb}";
        return string.IsNullOrEmpty(reason) ? outcome : $"{outcome}. {reason}";
    }

    public static Severity Severity(int done, int failed)
    {
        if (failed == 0)
        {
            return MudBlazor.Severity.Success;
        }

        return done == 0 ? MudBlazor.Severity.Error : MudBlazor.Severity.Warning;
    }
}
