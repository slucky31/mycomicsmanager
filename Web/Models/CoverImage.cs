using System.Globalization;
using System.Text.RegularExpressions;

namespace Web.Models;

// The four sizes of a cover (#1244), all 2:3. The widths of wwwroot/css/theme.css (--mcm-cover-*) are the CSS ones.
public enum CoverSize
{
    ExtraSmall,
    Small,
    Medium,
    Large,
}

public static partial class CoverImage
{
    private const string CloudinaryHost = "res.cloudinary.com";
    private const string UploadPath = "/image/upload/";

    // Pixels asked to Cloudinary: about three times the CSS width of the size, for the screens of the phones.
    private static int PixelWidth(CoverSize size) => size switch
    {
        CoverSize.ExtraSmall => 120,
        CoverSize.Small => 216,
        CoverSize.Medium => 400,
        _ => 480,
    };

    // A Cloudinary cover is asked at the size it is shown, cropped to 2:3, in the best format of the browser.
    // Any other link (OpenLibrary, Bubble, a local file) is kept as it is.
    public static string Sized(string? imageLink, CoverSize size)
    {
        if (string.IsNullOrEmpty(imageLink)
            || !Uri.TryCreate(imageLink, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Host, CloudinaryHost, StringComparison.OrdinalIgnoreCase))
        {
            return imageLink ?? string.Empty;
        }

        var upload = imageLink.IndexOf(UploadPath, StringComparison.Ordinal);
        if (upload < 0)
        {
            return imageLink;
        }

        var rest = imageLink[(upload + UploadPath.Length)..];
        var firstSegment = rest.Split('/', 2)[0];
        if (Transformation().IsMatch(firstSegment))
        {
            return imageLink;
        }

        var width = PixelWidth(size).ToString(CultureInfo.InvariantCulture);
        return $"{imageLink[..(upload + UploadPath.Length)]}c_fill,ar_2:3,w_{width},f_auto,q_auto/{rest}";
    }

    // A segment such as "c_fill,w_200": the link already carries its transformation.
    [GeneratedRegex("^[a-z]{1,3}_[^,/]+(,[a-z]{1,3}_[^,/]+)*$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex Transformation();
}
