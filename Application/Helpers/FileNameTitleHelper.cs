using Domain.FeedImports;

namespace Application.Helpers;

// Turns a file name into a readable serie / title / volume when nothing better is known (#1270):
// "Ashita_no_Joe.T08.[BD.fr].(2024).cbz" → serie and title "Ashita no Joe", volume 8.
public static class FileNameTitleHelper
{
    public static ParsedComicTitle Parse(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).Trim();
        // The parser drops the [..] and (..) groups, keeping only those that carry the volume ("(Tome 12)").
        var parsed = ComicTitleParser.Parse(name.Replace('_', ' ').Replace('.', ' '));

        // A name made only of bracket groups, or only of the volume, would leave no serie: keep it as it is.
        var serie = parsed.Serie ?? name;
        return new ParsedComicTitle(serie, parsed.Title ?? serie, parsed.Volume);
    }
}
