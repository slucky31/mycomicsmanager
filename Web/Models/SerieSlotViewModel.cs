using Application.Series;
using Domain.Series;

namespace Web.Models;

// One place of the series page (#1205): a book owned, or volumes missing between them.
public sealed record SerieSlotViewModel(string Key, string Badge, string? Title, string ImageLink, string? Href)
{
    public bool IsMissing => Href is null;

    public static SerieSlotViewModel Owned(SerieBookRow book)
    {
        ArgumentNullException.ThrowIfNull(book);
        return new SerieSlotViewModel(book.BookId.ToString(), $"#{book.VolumeNumber}", book.Title, book.ImageLink, $"/books/{book.BookId}");
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
