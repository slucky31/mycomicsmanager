namespace Web.Models;

// The book whose actions are shown (#1220): what the head of the menu shows of it.
public sealed record BookActionsTarget(Guid Id, string Title, string SerieAndVolume, string ImageLink)
{
    // "Blacksad · #4", the volume left out when the book has none.
    public static string SerieAndVolumeOf(string serie, int volumeNumber) => string.Join(" · ",
        new[] { serie, volumeNumber > 0 ? $"#{volumeNumber}" : null }.Where(part => !string.IsNullOrWhiteSpace(part)));

    public static BookActionsTarget From(BookListItemViewModel book)
    {
        ArgumentNullException.ThrowIfNull(book);
        return new BookActionsTarget(book.Id, book.Title, book.SerieAndVolume, book.ImageLink);
    }
}
