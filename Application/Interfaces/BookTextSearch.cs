using System.Globalization;
using Application.Helpers;

namespace Application.Interfaces;

// What is known of a book without ISBN (#1237): the sources are searched by its title, its series and its volume.
public sealed record BookTextQuery(string? Title, string? Serie, int? VolumeNumber)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Serie);

    // "Astérix 3 Astérix et les Goths": the words a full-text search looks for.
    public string ToText()
    {
        string?[] parts =
        [
            Serie?.Trim(),
            VolumeNumber is > 0 ? VolumeNumber.Value.ToString(CultureInfo.InvariantCulture) : null,
            string.Equals(Title?.Trim(), Serie?.Trim(), StringComparison.OrdinalIgnoreCase) ? null : Title?.Trim(),
        ];
        return string.Join(' ', parts.Where(part => !string.IsNullOrEmpty(part)));
    }
}

// An edition a source found for a text search; Isbn is null when the source gives none (an old edition).
public sealed record BookCandidate(
    string Title,
    string? Serie,
    int? VolumeNumber,
    IReadOnlyList<string> Publishers,
    DateOnly? PublishDate,
    string? Isbn,
    Uri? CoverUrl)
{
    // Only a valid ISBN can be searched on every source, then saved with the book.
    public static string? ValidIsbn(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IsbnHelper.IsValidISBN(value) ? IsbnHelper.NormalizeIsbn(value) : null;
}

public sealed record BookCandidatesResult(IReadOnlyList<BookCandidate> Candidates, bool Failed = false)
{
    // A source gives at most this many editions: the user picks one in a short list.
    public const int MaxCandidates = 10;

    public static readonly BookCandidatesResult None = new([]);

    // The source could not be searched (error, timeout, blocked): unlike None, it may know the book.
    public static readonly BookCandidatesResult Failure = new([], Failed: true);

    public bool Found => Candidates.Count > 0;

    // The editions of the volume searched first, then those with an ISBN; otherwise the order of the source.
    public BookCandidatesResult RankedFor(BookTextQuery query) =>
        this with
        {
            Candidates = [.. Candidates
                .OrderByDescending(candidate => query.VolumeNumber is > 0 && candidate.VolumeNumber == query.VolumeNumber)
                .ThenByDescending(candidate => candidate.Isbn is not null)
                .Take(MaxCandidates)],
        };
}
