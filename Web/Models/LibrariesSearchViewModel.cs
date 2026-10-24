using Application.Books.Search;

namespace Web.Models;

// What the search of the libraries page found (#1209): the series, then the books, each with its library.
public sealed record LibrariesSearchViewModel(
    IReadOnlyList<SerieSearchItemViewModel> Series,
    IReadOnlyList<BookSearchItemViewModel> Books,
    int BookCount)
{
    public static readonly LibrariesSearchViewModel Empty = new([], [], 0);

    public bool HasResults => Series.Count > 0 || Books.Count > 0;

    public int RemainingBookCount => Math.Max(0, BookCount - Books.Count);

    public string SeriesHeading => $"Series · {Series.Count}";

    public string BooksHeading => $"Books · {BookCount}";

    public string MoreLabel => RemainingBookCount == 1 ? "Show 1 more book" : $"Show {RemainingBookCount} more books";

    public static LibrariesSearchViewModel From(BookSearchResult result, IReadOnlyCollection<LibraryCardViewModel> libraries)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(libraries);

        var byId = libraries.ToDictionary(l => l.Id);
        return new LibrariesSearchViewModel(
            [.. result.Series.Select(s => SerieSearchItemViewModel.From(s, LibraryOf(byId, s.LibraryId)))],
            [.. result.Books.Select(b => BookSearchItemViewModel.From(b, LibraryOf(byId, b.LibraryId)))],
            result.BookCount);
    }

    // A library created since the page was loaded is not known: its name stays blank.
    private static SearchLibrary LibraryOf(Dictionary<Guid, LibraryCardViewModel> libraries, Guid id) =>
        libraries.TryGetValue(id, out var library)
            ? new SearchLibrary(library.Name, library.Color, library.Icon)
            : SearchLibrary.Unknown;
}

// The library of a result, as its chip shows it.
public sealed record SearchLibrary(string Name, string Color, string Icon)
{
    public static readonly SearchLibrary Unknown = new(string.Empty, "#5C6070", string.Empty);
}

public sealed record SerieSearchItemViewModel(string Name, SearchLibrary Library, string Details, string Href)
{
    public static SerieSearchItemViewModel From(SerieSearchHit hit, SearchLibrary library)
    {
        ArgumentNullException.ThrowIfNull(hit);
        return new(hit.Name, library, SerieDisplay.VolumeCount(hit.BookCount), SerieDisplay.SerieHref(hit.LibraryId, hit.Name));
    }
}

public sealed record BookSearchItemViewModel(Guid Id, string Title, string SerieAndVolume, string ImageLink, SearchLibrary Library)
{
    public string Href => $"/books/{Id}";

    public static BookSearchItemViewModel From(BookSearchHit hit, SearchLibrary library)
    {
        ArgumentNullException.ThrowIfNull(hit);

        // "Blacksad · #4", as in the list of a library.
        var serieAndVolume = string.Join(" · ",
            new[] { hit.Serie, hit.VolumeNumber > 0 ? $"#{hit.VolumeNumber}" : null }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return new(hit.Id, hit.Title, serieAndVolume, hit.ImageLink, library);
    }
}
