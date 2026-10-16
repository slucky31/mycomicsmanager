using System.Reflection;
using System.Text;
using MudBlazor;

namespace Web.Models;

// An icon a library can have: its Material name ("CollectionsBookmark") and the name read to the user ("Collections bookmark").
public sealed record LibraryIconChoice(string Name, string Label)
{
    public string Svg => LibraryIcons.Resolve(Name);
}

// The Material icons a library can have (E4, T12): a few suggestions, and all of them by a search.
public static class LibraryIcons
{
    public const int MaxResults = 40;

    private static readonly Dictionary<string, string> s_icons =
        typeof(Icons.Material.Filled)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!, StringComparer.Ordinal);

    private static readonly LibraryIconChoice[] s_suggested =
    [
        new("CollectionsBookmark", "Collection"),
        new("MenuBook", "Book"),
        new("LibraryBooks", "Books"),
        new("Star", "Star"),
        new("Favorite", "Heart"),
        new("Bookmark", "Bookmark"),
        new("Folder", "Folder"),
        new("Archive", "Archive"),
        new("TabletAndroid", "Tablet"),
        new("Home", "Home"),
        new("Flag", "Flag"),
        new("Bolt", "Bolt"),
        new("DarkMode", "Moon"),
        new("Public", "Globe"),
        new("CardGiftcard", "Gift"),
        new("TheaterComedy", "Mask"),
    ];

    public static IReadOnlyList<LibraryIconChoice> Suggested => s_suggested;

    // The SVG of an icon; a star when the name is unknown.
    public static string Resolve(string? name)
        => !string.IsNullOrEmpty(name) && s_icons.TryGetValue(name, out var svg) ? svg : Icons.Material.Filled.Star;

    public static bool Exists(string? name) => !string.IsNullOrEmpty(name) && s_icons.ContainsKey(name);

    // The name of a suggestion, or the words of the Material name: "AutoStories" is "Auto stories".
    public static string LabelOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var suggestion = Array.Find(s_suggested, choice => choice.Name == name);
        return suggestion?.Label ?? Words(name);
    }

    public static LibraryIconChoice ChoiceOf(string name) => new(name, LabelOf(name));

    // The suggestions, with the icon chosen first when it is not one of them; else the icons whose name has the words searched.
    public static IReadOnlyList<LibraryIconChoice> Search(string? query, string? chosen)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Exists(chosen) && !Array.Exists(s_suggested, choice => choice.Name == chosen)
                ? [ChoiceOf(chosen!), .. s_suggested]
                : s_suggested;
        }

        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return [.. s_icons.Keys
            .Where(name => Array.TrueForAll(words, word => name.Contains(word, StringComparison.OrdinalIgnoreCase)
                || LabelOf(name).Contains(word, StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal)
            .Take(MaxResults)
            .Select(ChoiceOf)];
    }

    // Above the grid: "Suggested", or how many icons were found.
    public static string ResultsCaption(string? query, int count) => (string.IsNullOrWhiteSpace(query), count) switch
    {
        (true, _) => "Suggested",
        (false, MaxResults) => $"First {MaxResults} icons",
        (false, 1) => "1 icon",
        _ => $"{count} icons",
    };

    private static string Words(string name)
    {
        var words = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && (char.IsUpper(c) || (char.IsDigit(c) && !char.IsDigit(name[i - 1]))))
            {
                words.Append(' ');
                c = char.ToLowerInvariant(c);
            }

            words.Append(c);
        }

        return words.ToString();
    }
}
