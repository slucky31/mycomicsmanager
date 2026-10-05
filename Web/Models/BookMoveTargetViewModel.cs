using System.Globalization;
using Application.Books.MoveTargets;

namespace Web.Models;

public sealed record BookMoveTargetViewModel(Guid Id, string Name, string Color, string? Hint, bool IsSuggested)
{
    public static IReadOnlyList<BookMoveTargetViewModel> From(BookMoveTargets targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        return targets.Libraries
            .Select(l => new BookMoveTargetViewModel(
                l.Id,
                l.Name,
                l.Color,
                l.SameSerieCount switch
                {
                    0 => null,
                    1 => "1 book of this series",
                    _ => string.Create(CultureInfo.InvariantCulture, $"{l.SameSerieCount} books of this series")
                },
                l.Id == targets.SuggestedLibraryId))
            .ToList();
    }
}
