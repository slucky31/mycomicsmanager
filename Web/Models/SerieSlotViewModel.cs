using Application.Series;
using Domain.Series;

namespace Web.Models;

// One place of the series page (#1205): a book owned, with its actions (#1220), or volumes missing between them.
public sealed record SerieSlotViewModel(string Key, string Badge, string? Title, string ImageLink, string? Href, BookActionsTarget? Book = null)
{
    public bool IsMissing => Book is null;

    public static SerieSlotViewModel Owned(SerieBookRow book)
    {
        ArgumentNullException.ThrowIfNull(book);
        var target = new BookActionsTarget(book.BookId, book.Title, BookActionsTarget.SerieAndVolumeOf(book.Serie, book.VolumeNumber), book.ImageLink);
        return new SerieSlotViewModel(book.BookId.ToString(), $"#{book.VolumeNumber}", book.Title, book.ImageLink, $"/books/{book.BookId}", target);
    }

    public static SerieSlotViewModel Missing(VolumeRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        return new SerieSlotViewModel($"missing-{range.First}", SerieDisplay.RangeLabel(range), null, string.Empty, null);
    }

    // The books by volume, each run of missing volumes in its place among them.
    public static IReadOnlyList<SerieSlotViewModel> Of(IReadOnlyList<SerieBookRow> orderedBooks, IReadOnlyList<VolumeRange> missing)
    {
        ArgumentNullException.ThrowIfNull(orderedBooks);
        ArgumentNullException.ThrowIfNull(missing);

        var slots = new List<SerieSlotViewModel>(orderedBooks.Count + missing.Count);
        var nextMissing = 0;
        foreach (var book in orderedBooks)
        {
            while (nextMissing < missing.Count && missing[nextMissing].First < book.VolumeNumber)
            {
                slots.Add(Missing(missing[nextMissing++]));
            }

            slots.Add(Owned(book));
        }

        slots.AddRange(missing.Skip(nextMissing).Select(Missing));
        return slots;
    }
}
