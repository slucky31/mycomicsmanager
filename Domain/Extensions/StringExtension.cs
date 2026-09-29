using System.Globalization;
using System.Text;

namespace Domain.Extensions;

public static class StringExtension
{
    public static string RemoveDiacritics(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }
        var normalizedString = str.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}



