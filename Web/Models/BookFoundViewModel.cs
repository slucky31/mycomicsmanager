using Application.Interfaces;

namespace Web.Models;

// The book a catalogue knows for the scanned or typed ISBN, shown before it is added (ADR-0021).
public sealed record BookFoundViewModel
{
    public required string Title { get; init; }
    public required string ImageLink { get; init; }

    // "Volume 1", with the series when it is not the title.
    public string? SerieAndVolume { get; init; }
    public string? Authors { get; init; }

    // "Delcourt · 2019 · 271 pages".
    public string? Publication { get; init; }
    public required string IsbnLabel { get; init; }

    // "Found on BnF", nothing when the catalogue is unknown.
    public string? SourceLabel { get; init; }

    public string CoverAlt => $"Cover of {Title}";

    public static BookFoundViewModel From(ComicSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new BookFoundViewModel
        {
            Title = result.Title,
            ImageLink = result.ImageUrl,
            SerieAndVolume = BookText.SerieAndVolume(result.Serie, result.Title, result.VolumeNumber),
            Authors = string.IsNullOrWhiteSpace(result.Authors) ? null : result.Authors,
            Publication = BookText.Join(result.Publishers, result.PublishDate?.Year.ToString(System.Globalization.CultureInfo.InvariantCulture), BookText.PagesLabel(result.NumberOfPages)),
            IsbnLabel = $"ISBN {result.Isbn}",
            SourceLabel = string.IsNullOrWhiteSpace(result.Source) ? null : $"Found on {result.Source}",
        };
    }
}
