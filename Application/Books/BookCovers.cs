using System.Text.RegularExpressions;

namespace Application.Books;

// Every cover is an image of its own book (#1225): <folder>/<book id>, in the folder of the Cloudinary settings.
// Before, the covers were named after the ISBN, shared by the books of every user, and the first pages of the
// archives went to digital-covers.
public static partial class BookCovers
{
    public const string LegacyDigitalFolder = "digital-covers";

    private const string CloudinaryHost = "res.cloudinary.com";
    private const string UploadPath = "/image/upload/";

    public static string PublicId(Guid bookId) => bookId.ToString();

    // True when the link is the image of this book in this folder.
    public static bool IsStoredFor(string? imageLink, string folder, Guid bookId) =>
        string.Equals(PublicIdOf(imageLink), $"{folder}/{PublicId(bookId)}", StringComparison.OrdinalIgnoreCase);

    // True when the link is an image of Cloudinary.
    public static bool IsCloudinary(string? imageLink) => PublicIdOf(imageLink) is not null;

    // The public id of a Cloudinary link ("covers/0193…"): what follows /image/upload/, without the transformations,
    // the version and the extension. Null for any other link.
    public static string? PublicIdOf(string? imageLink)
    {
        if (!Uri.TryCreate(imageLink, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Host, CloudinaryHost, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = uri.AbsolutePath;
        var upload = path.IndexOf(UploadPath, StringComparison.Ordinal);
        if (upload < 0)
        {
            return null;
        }

        var segments = path[(upload + UploadPath.Length)..].Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(segment => Transformation().IsMatch(segment))
            .SkipWhile(segment => Version().IsMatch(segment))
            .ToArray();
        if (segments.Length == 0)
        {
            return null;
        }

        var last = segments[^1];
        var dot = last.LastIndexOf('.', StringComparison.Ordinal);
        segments[^1] = dot > 0 ? last[..dot] : last;
        return Uri.UnescapeDataString(string.Join('/', segments));
    }

    // A segment such as "c_fill,w_200".
    [GeneratedRegex("^[a-z]{1,3}_[^,/]+(,[a-z]{1,3}_[^,/]+)*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex Transformation();

    // The version of the image, such as "v1712345678".
    [GeneratedRegex("^v[0-9]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex Version();
}
