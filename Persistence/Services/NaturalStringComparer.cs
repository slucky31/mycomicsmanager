namespace Persistence.Services;

/// <summary>
/// Case-insensitive comparison where digit runs are compared by numeric value,
/// so "page2.jpg" sorts before "page10.jpg".
/// </summary>
internal sealed class NaturalStringComparer : IComparer<string?>
{
    public static readonly NaturalStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            var result = char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j])
                ? CompareNumbers(x, ref i, y, ref j)
                : char.ToUpperInvariant(x[i++]).CompareTo(char.ToUpperInvariant(y[j++]));

            if (result != 0)
            {
                return result;
            }
        }

        return (x.Length - i).CompareTo(y.Length - j);
    }

    private static int CompareNumbers(string x, ref int i, string y, ref int j)
    {
        var xDigits = ReadDigits(x, ref i);
        var yDigits = ReadDigits(y, ref j);

        // Leading zeros carry no value: "007" and "7" compare equal here.
        var xValue = xDigits.TrimStart('0');
        var yValue = yDigits.TrimStart('0');
        if (xValue.Length != yValue.Length)
        {
            return xValue.Length.CompareTo(yValue.Length);
        }

        var result = xValue.CompareTo(yValue, StringComparison.Ordinal);
        return result != 0 ? result : xDigits.Length.CompareTo(yDigits.Length);
    }

    private static ReadOnlySpan<char> ReadDigits(string s, ref int index)
    {
        var start = index;
        while (index < s.Length && char.IsAsciiDigit(s[index]))
        {
            index++;
        }

        return s.AsSpan(start, index - start);
    }
}
