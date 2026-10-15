namespace Web.Models;

// Where a value of the "Import info from the web" page comes from.
public enum BookSource
{
    Current,
    Bnf,
    Bedetheque,
    Bubble,
    OpenLibrary,
    Google
}

public static class BookSources
{
    public static string Name(BookSource source) => source switch
    {
        BookSource.Bnf => "BnF",
        BookSource.Bedetheque => "Bedetheque",
        BookSource.Bubble => "Bubble",
        BookSource.OpenLibrary => "OpenLibrary",
        BookSource.Google => "Google Books",
        _ => "Current",
    };
}

// A field compared on the page: its heading, and how it is named in the "already match" sentence.
public sealed record BookFieldDefinition(string Key, string Label, string MatchLabel, bool IsImage = false)
{
    public static readonly IReadOnlyList<BookFieldDefinition> All =
    [
        new(BookFieldKeys.Title, "Title", "title"),
        new(BookFieldKeys.Serie, "Series", "series"),
        new(BookFieldKeys.VolumeNumber, "Volume", "volume"),
        new(BookFieldKeys.Authors, "Authors", "authors"),
        new(BookFieldKeys.Publishers, "Publisher", "publisher"),
        new(BookFieldKeys.PublishDate, "Publish date", "publish date"),
        new(BookFieldKeys.NumberOfPages, "Pages", "pages"),
        new(BookFieldKeys.Cover, "Cover", "cover", IsImage: true),
    ];
}

// One value to pick, with every source that gives it (the current value comes first).
public sealed record BookFieldOption(string? Value, IReadOnlyList<BookSource> Sources)
{
    public BookSource Source => Sources[0];

    public bool IsCurrent => Source == BookSource.Current;

    public string SourcesLabel => string.Join(" · ", Sources.Select(BookSources.Name));
}

public enum BookFieldMatch
{
    // A source gives another value: the field is shown with its options.
    Differs,

    // Every source that knows the field gives the current value.
    Matches,

    // No source knows the field.
    Unknown
}

public sealed record BookFieldComparison(BookFieldDefinition Field, IReadOnlyList<BookFieldOption> Options, BookFieldMatch Match)
{
    public static BookFieldComparison Compare(BookFieldDefinition field, string? current, IEnumerable<(BookSource Source, string? Value)> candidates)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(candidates);

        var currentValue = Normalize(current);
        var options = new List<(string? Value, List<BookSource> Sources)> { (currentValue, [BookSource.Current]) };
        var matches = false;

        foreach (var (source, value) in candidates)
        {
            var candidate = Normalize(value);
            if (candidate is null)
            {
                continue;
            }

            if (string.Equals(candidate, currentValue, StringComparison.Ordinal))
            {
                matches = true;
                continue;
            }

            var same = options.FindIndex(1, o => string.Equals(o.Value, candidate, StringComparison.Ordinal));
            if (same >= 0)
            {
                options[same].Sources.Add(source);
            }
            else
            {
                options.Add((candidate, [source]));
            }
        }

        return new BookFieldComparison(field, options.Select(o => new BookFieldOption(o.Value, o.Sources)).ToList(), MatchOf(options.Count, matches));
    }

    private static BookFieldMatch MatchOf(int optionCount, bool matches)
    {
        if (optionCount > 1)
        {
            return BookFieldMatch.Differs;
        }

        return matches ? BookFieldMatch.Matches : BookFieldMatch.Unknown;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// The fields of a book compared with what the web sources know: only the differences are offered to pick from (S6).
/// </summary>
public sealed record BookDetailsComparison(IReadOnlyList<BookFieldComparison> Fields)
{
    public IReadOnlyList<BookFieldComparison> Differences { get; } =
        Fields.Where(f => f.Match == BookFieldMatch.Differs).ToList();

    // "Title, series, volume, authors and publisher already match."
    public string? MatchSummary
    {
        get
        {
            var matching = Fields.Where(f => f.Match == BookFieldMatch.Matches).Select(f => f.Field.MatchLabel).ToList();
            if (matching.Count == 0)
            {
                return null;
            }

            var list = matching.Count == 1
                ? matching[0]
                : $"{string.Join(", ", matching.Take(matching.Count - 1))} and {matching[^1]}";
            return $"{char.ToUpperInvariant(list[0])}{list[1..]} already {(matching.Count == 1 ? "matches" : "match")}.";
        }
    }

    public string DifferencesSummary => Differences.Count switch
    {
        0 => "No differences found.",
        1 => "1 difference found.",
        var count => $"{count} differences found.",
    };

    // The fields where the value picked is not the current one.
    public int CountChanges(IReadOnlyDictionary<string, BookSource> selected)
    {
        ArgumentNullException.ThrowIfNull(selected);

        return Differences.Count(f =>
            selected.TryGetValue(f.Field.Key, out var source)
            && f.Options.Any(o => !o.IsCurrent && o.Sources.Contains(source)));
    }

    public static string ApplyLabel(int changes) => changes switch
    {
        0 => "Apply changes",
        1 => "Apply 1 change",
        _ => $"Apply {changes} changes",
    };
}
