using System.Text.RegularExpressions;

namespace Application.Helpers;

// Turns a file name into a readable serie / title when nothing better is known (#1270):
// "Ashita.no.Joe.T08.[BD.fr].(2024).cbz" → "Ashita no Joe T08".
public static partial class FileNameTitleHelper
{
    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*\)", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex BracketGroupPattern();

    [GeneratedRegex(@"\s{2,}", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MultiSpacePattern();

    public static string ToTitle(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var cleaned = BracketGroupPattern().Replace(name, " ").Replace('.', ' ');
        cleaned = MultiSpacePattern().Replace(cleaned, " ").Trim();

        // A name made only of bracket groups would leave nothing: keep it as it is.
        return cleaned.Length == 0 ? name.Trim() : cleaned;
    }
}
