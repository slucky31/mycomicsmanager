namespace Application.Helpers;

public static class IsbnHelper
{
    public static bool IsValidISBN(string isbn)
    {
        if (string.IsNullOrEmpty(isbn))
        {
            return false;
        }

        // Remove any dashes, spaces, and convert to uppercase
        var cleanIsbn = NormalizeIsbn(isbn);

        // Check ISBN-10
        if (cleanIsbn.Length == 10)
        {
            return IsValidISBN10(cleanIsbn);
        }

        // Check ISBN-13
        if (cleanIsbn.Length == 13)
        {
            return IsValidISBN13(cleanIsbn);
        }

        return false;
    }

    public static bool IsValidISBN10(string? isbn)
    {
        if (string.IsNullOrEmpty(isbn))
        {
            return false;
        }

        // First 9 characters must be digits, last can be digit or X
        for (var i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return false;
            }
        }

        var lastChar = isbn[9];
        if (!char.IsDigit(lastChar) && lastChar != 'X')
        {
            return false;
        }

        // Calculate checksum
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (isbn[i] - '0') * (10 - i);
        }

        // Add the check digit
        if (lastChar == 'X')
        {
            sum += 10;
        }
        else
        {
            sum += lastChar - '0';
        }

        return sum % 11 == 0;
    }

    public static bool IsValidISBN13(string? isbn)
    {
        if (string.IsNullOrEmpty(isbn))
        {
            return false;
        }

        // All characters must be digits
        for (var i = 0; i < 13; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return false;
            }
        }

        // Calculate checksum
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = isbn[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        var actualCheckDigit = isbn[12] - '0';

        return checkDigit == actualCheckDigit;
    }

    public static string NormalizeIsbn(string isbn)
    {
        if (string.IsNullOrEmpty(isbn))
        {
            return string.Empty;
        }
        return isbn.Replace("-", "", StringComparison.Ordinal)
                   .Replace(" ", "", StringComparison.Ordinal)
                   .Replace(Environment.NewLine, "", StringComparison.Ordinal)
                   .Trim()
                   .ToUpperInvariant();
    }

    // Returns the ISBN-13 formatted with dashes using official ISBN range rules for
    // group 978-2 (France) and 979-10 (France). Returns null for unsupported prefixes.
    public static string? ToHyphenatedIsbn(string isbn)
    {
        if (isbn.Length != 13)
        {
            return null;
        }
        if (isbn.StartsWith("9782", StringComparison.Ordinal))
        {
            var body = isbn[4..12];
            var check = isbn[12];
            var pubLen = PublisherLength978Group2(body);
            if (pubLen == 0)
            {
                return null;
            }
            return $"978-2-{body[..pubLen]}-{body[pubLen..]}-{check}";
        }
        if (isbn.StartsWith("97910", StringComparison.Ordinal))
        {
            var body = isbn[5..12];
            var check = isbn[12];
            var pubLen = PublisherLength979Group10(body);
            if (pubLen == 0)
            {
                return null;
            }
            return $"979-10-{body[..pubLen]}-{body[pubLen..]}-{check}";
        }
        return null;
    }

    // Strips the EAN prefix (3 digits) and check digit from an ISBN-13, returning
    // the 9-digit body formatted as X-XXX-XXXXX. Returns null for non-978/979 ISBNs.
    public static string? ToShortIsbn(string isbn)
    {
        if (isbn.Length != 13)
        {
            return null;
        }
        if (!isbn.StartsWith("978", StringComparison.Ordinal) &&
            !isbn.StartsWith("979", StringComparison.Ordinal))
        {
            return null;
        }
        var nine = isbn[3..12];
        return $"{nine[0]}-{nine[1..4]}-{nine[4..]}";
    }

    private static int PublisherLength978Group2(string body)
    {
        if (!TryParseSlices(body, 2, 3, 4, 5, 6, 7, out var p2, out var p3, out var p4, out var p5, out var p6, out var p7))
        {
            return 0;
        }
        if (p2 <= 19)                        { return 2; }
        if (p3 is >= 200 and <= 699)         { return 3; }
        if (p4 is >= 7000 and <= 8499)       { return 4; }
        if (p5 is >= 85000 and <= 89999)     { return 5; }
        if (p6 is >= 900000 and <= 949999)   { return 6; }
        if (p7 is >= 9500000 and <= 9999999) { return 7; }
        return 0;
    }

    private static int PublisherLength979Group10(string body)
    {
        if (!TryParseSlices(body, 2, 3, 4, 5, 6, 0, out var p2, out var p3, out var p4, out var p5, out var p6, out _))
        {
            return 0;
        }
        if (p2 <= 19)                      { return 2; }
        if (p3 is >= 200 and <= 699)       { return 3; }
        if (p4 is >= 7000 and <= 8699)     { return 4; }
        if (p5 is >= 87000 and <= 89999)   { return 5; }
        if (p6 is >= 900000 and <= 974999) { return 6; }
        return 0;
    }

    private static bool TryParseSlices(
        string body,
        int l2, int l3, int l4, int l5, int l6, int l7,
        out int p2, out int p3, out int p4, out int p5, out int p6, out int p7)
    {
        p2 = p3 = p4 = p5 = p6 = p7 = 0;
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        return (l2 == 0 || int.TryParse(body[..l2], culture, out p2))
            && (l3 == 0 || int.TryParse(body[..l3], culture, out p3))
            && (l4 == 0 || int.TryParse(body[..l4], culture, out p4))
            && (l5 == 0 || int.TryParse(body[..l5], culture, out p5))
            && (l6 == 0 || int.TryParse(body[..l6], culture, out p6))
            && (l7 == 0 || int.TryParse(body[..l7], culture, out p7));
    }
}
