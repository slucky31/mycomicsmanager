using Application.Books;
using Domain.FeedImports;

namespace Application.FeedImports.Analysis;

public enum DuplicateMatchKind
{
    None,
    Probable,
    Certain
}

public sealed record DuplicateMatch(DuplicateMatchKind Kind, BookIdentityDto? Book)
{
    public static DuplicateMatch None { get; } = new(DuplicateMatchKind.None, null);
}

// Certain: same ISBN, or same normalized serie and same volume.
// Probable: very close serie (SerieMatcher.ProbableMatchThreshold) and same volume, or same serie with an unknown volume.
public static class DuplicateBookFinder
{
    public static DuplicateMatch Find(ParsedComicTitle parsed, IReadOnlyList<BookIdentityDto> books)
    {
        ArgumentNullException.ThrowIfNull(parsed);
        ArgumentNullException.ThrowIfNull(books);

        var isbn = NormalizeIsbn(parsed.Isbn);
        if (isbn is not null)
        {
            var byIsbn = books.FirstOrDefault(b => NormalizeIsbn(b.Isbn) == isbn);
            if (byIsbn is not null)
            {
                return new DuplicateMatch(DuplicateMatchKind.Certain, byIsbn);
            }
        }

        if (!parsed.HasSerie)
        {
            return DuplicateMatch.None;
        }

        BookIdentityDto? probable = null;
        var bestSimilarity = 0d;
        foreach (var book in books)
        {
            var similarity = SerieMatcher.Similarity(parsed.Serie, book.Serie);
            var sameVolume = parsed.Volume.HasValue && parsed.Volume.Value == book.VolumeNumber;
            if (similarity >= 1 && sameVolume)
            {
                return new DuplicateMatch(DuplicateMatchKind.Certain, book);
            }

            var isProbable = (similarity >= 1 && !parsed.Volume.HasValue) ||
                             (similarity >= SerieMatcher.ProbableMatchThreshold && sameVolume);
            if (isProbable && similarity > bestSimilarity)
            {
                probable = book;
                bestSimilarity = similarity;
            }
        }

        return probable is null ? DuplicateMatch.None : new DuplicateMatch(DuplicateMatchKind.Probable, probable);
    }

    private static string? NormalizeIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        var normalized = new string(isbn.Where(c => char.IsDigit(c) || c is 'X' or 'x').ToArray()).ToUpperInvariant();
        return normalized.Length is 10 or 13 ? normalized : null;
    }
}
