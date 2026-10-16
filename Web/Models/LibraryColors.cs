namespace Web.Models;

// A color of the palette of the libraries, with the name read to the user.
public sealed record LibraryColor(string Hex, string Name);

// The colors a library can have (E3, T12).
public static class LibraryColors
{
    private static readonly LibraryColor[] s_palette =
    [
        new("#5C6BC0", "Indigo"),
        new("#7B5EA7", "Purple"),
        new("#26A69A", "Teal"),
        new("#42A5F5", "Blue"),
        new("#66BB6A", "Green"),
        new("#FFCA28", "Amber"),
        new("#FF7043", "Orange"),
        new("#EC407A", "Pink"),
        new("#78909C", "Grey blue"),
        new("#8D6E63", "Brown"),
        new("#26C6DA", "Cyan"),
        new("#D4E157", "Lime"),
    ];

    public static IReadOnlyList<LibraryColor> Palette => s_palette;

    public static bool IsSelected(LibraryColor color, string? hex)
    {
        ArgumentNullException.ThrowIfNull(color);
        return string.Equals(color.Hex, hex, StringComparison.OrdinalIgnoreCase);
    }
}
