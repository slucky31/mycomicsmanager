using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Ardalis.GuardClauses;

namespace Domain.Extensions;

public static class StringExtension
{
    public static string? ToPascalCase(this string? str)
    {
        if (string.IsNullOrEmpty(str) || str.Length <= 1)
        {
            return str;
        }
        
        // Ajout d'une majuscule à chaque début de mot
        var txtInfo = CultureInfo.InvariantCulture.TextInfo;
        var stripStr = txtInfo.ToTitleCase(str);
            
        // Conservation uniquement des lettres et des chiffres
        // Source : https://stackoverflow.com/questions/7316258/how-to-get-only-letters-from-a-string-in-c/7316298
        stripStr = new string(stripStr.Where(c => Char.IsLetter(c) || Char.IsDigit(c)).ToArray());
            
        // Suppression des accents
        // Source : https://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net
        return stripStr.RemoveDiacritics();
    }
    
    public static string? ToCamlCase(this string? str)
    {
        if (string.IsNullOrEmpty(str) || str.Length <= 1)
        {
            return str;
        }

        var caml = str.ToPascalCase();
        Guard.Against.Null(caml);
        return char.ToLowerInvariant(caml[0]) + caml[1..];
    }
    
    public static string? RemoveDiacritics(this string? str) 
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

    public static string? Substract(this string? str2, string? str1)
    {
        if (string.IsNullOrEmpty(str2) || string.IsNullOrEmpty(str1))
        {
            return str2;
        }

        Guard.Against.NullOrEmpty(str2, str1);
        return str2.Replace(str1, "", StringComparison.InvariantCultureIgnoreCase);
    }

}



