using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Ardalis.GuardClauses;

namespace Web.Tests;

public static class AngleSharpExtensions
{
    public static string? TrimmedText(this IElement self)
    {
        Guard.Against.Null(self);
        return self.TextContent?.Trim();
    }
}
