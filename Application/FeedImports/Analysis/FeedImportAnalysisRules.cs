using System.Globalization;
using Application.Books;
using Application.Helpers;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.Analysis;

// Shared by the automatic analysis and the manual arbitration: parse a book, look for a duplicate, apply the transition.
public static class FeedImportAnalysisRules
{
    public static ParsedComicTitle ParseCandidate(DownloadCandidate candidate, string articleTitle, bool isOnlyBookOfArticle)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var article = ComicTitleParser.Parse(articleTitle);
        var file = ComicTitleParser.Parse(candidate.FileName ?? candidate.Label);
        var isbn = candidate.FileName is null ? null : FileNameIsbnExtractor.ExtractIsbn(candidate.FileName);

        // A single book is best described by the article title; in a multi-book article each file name describes its own book.
        return isOnlyBookOfArticle
            ? new ParsedComicTitle(article.Serie ?? file.Serie, article.Title ?? file.Title, article.Volume ?? file.Volume, isbn)
            : new ParsedComicTitle(file.Serie ?? article.Serie, file.Title, file.Volume, isbn);
    }

    public static Result ApplyDuplicateCheck(
        FeedImportDecision decision,
        DownloadCandidate candidate,
        ParsedComicTitle parsed,
        IReadOnlyList<BookIdentityDto> books,
        FeedImportDecidedBy decidedBy)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(candidate);

        var match = DuplicateBookFinder.Find(parsed, books);
        return match.Kind switch
        {
            DuplicateMatchKind.Certain => decision.MarkDuplicate(
                [candidate], parsed, match.Book!.Id, $"Doublon : « {Describe(match.Book)} » dans « {match.Book.LibraryName} ».", decidedBy),
            DuplicateMatchKind.Probable => decision.RequestArbitration(
                FeedImportArbitrationKind.ProbableDuplicate, [candidate], parsed, match.Book!.Id,
                $"Doublon probable : « {Describe(match.Book)} » dans « {match.Book.LibraryName} ».", decidedBy),
            _ => decision.RecordLinks([candidate], parsed, DescribeLinks(candidate), decidedBy)
        };
    }

    private static string Describe(BookIdentityDto book)
    {
        var serie = string.IsNullOrWhiteSpace(book.Serie) ? book.Title : book.Serie;
        return string.Create(CultureInfo.InvariantCulture, $"{serie} T{book.VolumeNumber}");
    }

    private static string DescribeLinks(DownloadCandidate candidate) => candidate.Mirrors.Count == 1
        ? "1 lien trouvé, aucun doublon."
        : string.Create(CultureInfo.InvariantCulture, $"{candidate.Mirrors.Count} miroirs trouvés, aucun doublon.");
}
