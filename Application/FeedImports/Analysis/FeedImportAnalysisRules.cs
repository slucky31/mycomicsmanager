using System.Globalization;
using Application.Books;
using Application.Helpers;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.Analysis;

// Shared by the automatic analysis and the manual arbitration: parse a book, look for a duplicate, apply the transition.
public static class FeedImportAnalysisRules
{
    public static ParsedComicTitle ParseCandidate(DownloadCandidate candidate, string articleTitle, bool isOnlyBookOfArticle, string? pageIsbn = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var article = ComicTitleParser.Parse(articleTitle);
        var file = ComicTitleParser.Parse(candidate.FileName ?? candidate.Label);
        var isbn = (candidate.FileName is null ? null : FileNameIsbnExtractor.ExtractIsbn(candidate.FileName)) ?? pageIsbn;

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
                [candidate], parsed, match.Book!.Id, $"Duplicate: \"{Describe(match.Book)}\" in \"{match.Book.LibraryName}\".", decidedBy),
            DuplicateMatchKind.Probable => decision.RequestArbitration(
                FeedImportArbitrationKind.ProbableDuplicate, [candidate], parsed, match.Book!.Id,
                $"Probable duplicate: \"{Describe(match.Book)}\" in \"{match.Book.LibraryName}\".", decidedBy),
            _ => decision.RecordLinks([candidate], parsed, DescribeLinks(candidate), decidedBy)
        };
    }

    // A decision reopened by the user (retry, correction): same rules as the automatic analysis, on the stored links.
    // Without any link the decision stays Pending and the next sync analyzes the article again.
    public static Result Reapply(FeedImportDecision decision, IReadOnlyList<BookIdentityDto> books, FeedImportDecidedBy decidedBy)
    {
        ArgumentNullException.ThrowIfNull(decision);

        var candidates = decision.GetCandidates();
        if (candidates.Count == 0)
        {
            return Result.Success();
        }

        if (candidates.Count > 1)
        {
            return decision.RequestArbitration(
                FeedImportArbitrationKind.AmbiguousLinks, candidates, StoredParsed(decision, candidates[0]), matchedBookId: null,
                "Several files: choose the right link.", decidedBy);
        }

        return ApplyDuplicateCheck(decision, candidates[0], StoredParsed(decision, candidates[0]), books, decidedBy);
    }

    // Keeps the user's corrections; parses the file again only when nothing was stored.
    private static ParsedComicTitle StoredParsed(FeedImportDecision decision, DownloadCandidate candidate)
    {
        if (decision.ParsedSerie is null && decision.ParsedTitle is null && decision.ParsedVolume is null)
        {
            return ParseCandidate(candidate, decision.EntryTitle, isOnlyBookOfArticle: decision.ItemIndex == 0);
        }

        var isbn = candidate.FileName is null ? null : FileNameIsbnExtractor.ExtractIsbn(candidate.FileName);
        return new ParsedComicTitle(decision.ParsedSerie, decision.ParsedTitle, decision.ParsedVolume, isbn);
    }

    private static string Describe(BookIdentityDto book)
    {
        var serie = string.IsNullOrWhiteSpace(book.Serie) ? book.Title : book.Serie;
        return string.Create(CultureInfo.InvariantCulture, $"{serie} T{book.VolumeNumber}");
    }

    private static string DescribeLinks(DownloadCandidate candidate) => candidate.Mirrors.Count == 1
        ? "1 link found, no duplicate."
        : string.Create(CultureInfo.InvariantCulture, $"{candidate.Mirrors.Count} mirrors found, no duplicate.");
}
