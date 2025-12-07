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
                   .ToUpperInvariant();
    }
}
