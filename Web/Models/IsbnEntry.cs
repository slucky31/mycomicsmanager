using Application.Helpers;

namespace Web.Models;

// The ISBN typed by hand: dashes and spaces are accepted, the digits are checked before the look up.
public static class IsbnEntry
{
    public const string Help = "10 or 13 digits. Dashes and spaces are ignored.";
    public const string Missing = "Enter the ISBN";
    public const string Invalid = "Enter a valid ISBN: 10 or 13 digits";

    // The ISBN to look up, or the error to show under the field.
    public static (string? Isbn, string? Error) Check(string? typed)
    {
        var isbn = IsbnHelper.NormalizeIsbn(typed ?? string.Empty).ToUpperInvariant();
        if (isbn.Length == 0)
        {
            return (null, Missing);
        }

        return IsbnHelper.IsValidISBN(isbn) ? (isbn, null) : (null, Invalid);
    }
}
