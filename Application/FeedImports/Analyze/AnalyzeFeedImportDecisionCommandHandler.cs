using Application.Abstractions.Messaging;
using Application.FeedImports.Analysis;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Extensions;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.FeedImports.Analyze;

// Pending decision -> fetch the article page -> extract and group links -> duplicate check per book.
// Ends in LinksExtracted, AwaitingArbitration, SkippedDuplicate or Failed; never downloads anything.
// Each file only offered on a host outside FeedImport:AllowedDownloadHosts becomes a Failed decision keeping its links.
public sealed class AnalyzeFeedImportDecisionCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IArticlePageFetcher pageFetcher,
    IEnumerable<IDownloadLinkExtractor> extractors,
    IBookReadService bookReadService,
    IUnitOfWork unitOfWork,
    IOptions<FeedImportSettings> feedImportSettings,
    ILogger<AnalyzeFeedImportDecisionCommandHandler> logger) : ICommandHandler<AnalyzeFeedImportDecisionCommand>
{
    public const string SourceStep = "Source";
    public const string FetchStep = "Page fetch";
    public const string ExtractionStep = "Link extraction";

    public async Task<Result> Handle(AnalyzeFeedImportDecisionCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.DecisionId == Guid.Empty)
        {
            return FeedImportError.BadRequest;
        }

        var decision = await decisionRepository.GetByIdAsync(command.DecisionId, cancellationToken);
        if (decision is null)
        {
            return FeedImportError.NotFound;
        }

        // Already analyzed (or a book split from an article, analyzed together with it): nothing to do.
        if (decision.Status != FeedImportDecisionStatus.Pending || decision.ItemIndex != 0)
        {
            return Result.Success();
        }

        var analysis = await AnalyzeAsync(decision, cancellationToken);
        if (analysis.IsFailure)
        {
            return analysis;
        }

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error! : Result.Success();
    }

    private async Task<Result> AnalyzeAsync(FeedImportDecision decision, CancellationToken cancellationToken)
    {
        var settings = feedImportSettings.Value;
        var pageUri = new Uri(decision.EntryUrl);
        if (!pageUri.Host.IsSameOrSubdomainOf(settings.AllowedSourceHosts))
        {
            return decision.Fail(SourceStep, $"{FeedImportError.SourceHostNotAllowed.Description} ({pageUri.Host})");
        }

        // Feed links are often plain http://: the page is always fetched over HTTPS.
        pageUri = ToHttps(pageUri);
        var htmlResult = await pageFetcher.GetHtmlAsync(pageUri, cancellationToken);
        if (htmlResult.IsFailure)
        {
            logger.LogWarning("Feed import: page {Url} could not be fetched: [{Code}] {Description}",
                pageUri, htmlResult.Error!.Code, htmlResult.Error.Description);
            return decision.Fail(FetchStep, htmlResult.Error.Description ?? FeedImportError.PageUnavailable.Description!);
        }

        var extractor = extractors.FirstOrDefault(e => !e.IsFallback && e.CanHandle(pageUri))
                        ?? extractors.First(e => e.IsFallback);
        var extraction = extractor.Extract(htmlResult.Value!, pageUri, settings.AllowedDownloadHosts);
        var unsupported = GroupUnsupportedLinks(extraction, decision.EntryTitle);
        if (extraction.Links.Count == 0 && unsupported.Count == 0)
        {
            return decision.Fail(ExtractionStep, "No link to an allowed host (FeedImport:AllowedDownloadHosts).");
        }

        var grouping = extraction.Links.Count == 0 ? null : DownloadLinkGrouper.Group(extraction.Links, decision.EntryTitle);
        // Ambiguous links stay on a single decision, until the user groups them.
        var supportedCount = grouping is null ? 0 : grouping.IsAmbiguous ? 1 : grouping.Candidates.Count;
        var decisions = CreateDecisions(decision, supportedCount + unsupported.Count);
        if (decisions.IsFailure)
        {
            return decisions.Error!;
        }

        // The ISBN printed on the page only describes the book when the article holds a single one.
        var isOnlyBook = decisions.Value!.Count == 1;
        var pageIsbn = isOnlyBook ? extraction.Isbn : null;
        var applied = await ApplySupportedAsync(decisions.Value[..supportedCount], grouping, isOnlyBook, pageIsbn, cancellationToken);
        return applied.IsFailure
            ? applied
            : FailUnsupported(decisions.Value[supportedCount..], unsupported, decision.EntryTitle, isOnlyBook, pageIsbn);
    }

    // Files only offered on hosts outside FeedImport:AllowedDownloadHosts, one candidate per file.
    // A file also served by an allowed host is not reported; neither is anything when an allowed link has no
    // file name, since it may be a mirror of any of them.
    private static IReadOnlyList<DownloadCandidate> GroupUnsupportedLinks(ArticleExtraction extraction, string articleTitle)
    {
        if (extraction.UnsupportedLinks.Count == 0 || extraction.Links.Any(l => l.FileName is null))
        {
            return [];
        }

        var served = extraction.Links.Select(l => DownloadLinkGrouper.NormalizeFileName(l.FileName!)).ToHashSet(StringComparer.Ordinal);
        var links = extraction.UnsupportedLinks.Where(l => !served.Contains(DownloadLinkGrouper.NormalizeFileName(l.FileName!))).ToList();
        return DownloadLinkGrouper.Group(links, articleTitle).Candidates;
    }

    // One decision per book: the original keeps the first book, the others become sibling decisions.
    private Result<List<FeedImportDecision>> CreateDecisions(FeedImportDecision decision, int count)
    {
        var decisions = new List<FeedImportDecision> { decision };
        for (var i = 1; i < count; i++)
        {
            var sibling = decision.CreateSibling(i);
            if (sibling.IsFailure)
            {
                return sibling.Error!;
            }
            decisionRepository.Add(sibling.Value!);
            decisions.Add(sibling.Value!);
        }

        return decisions;
    }

    private async Task<Result> ApplySupportedAsync(
        List<FeedImportDecision> decisions,
        LinkGroupingResult? grouping,
        bool isOnlyBook,
        string? pageIsbn,
        CancellationToken cancellationToken)
    {
        if (grouping is null)
        {
            return Result.Success();
        }

        if (grouping.IsAmbiguous)
        {
            var parsed = ComicTitleParser.Parse(decisions[0].EntryTitle);
            return decisions[0].RequestArbitration(
                FeedImportArbitrationKind.AmbiguousLinks, grouping.Candidates, parsed, matchedBookId: null,
                grouping.AmbiguityReason!, FeedImportDecidedBy.Auto);
        }

        var books = await bookReadService.ListIdentitiesByUserAsync(decisions[0].UserId, cancellationToken);
        for (var i = 0; i < decisions.Count; i++)
        {
            var candidate = grouping.Candidates[i];
            var parsed = FeedImportAnalysisRules.ParseCandidate(candidate, decisions[i].EntryTitle, isOnlyBook, pageIsbn);
            var applied = FeedImportAnalysisRules.ApplyDuplicateCheck(decisions[i], candidate, parsed, books, FeedImportDecidedBy.Auto);
            if (applied.IsFailure)
            {
                return applied;
            }
        }

        return Result.Success();
    }

    // Failed with their links: the user downloads these files by hand.
    private static Result FailUnsupported(
        List<FeedImportDecision> decisions,
        IReadOnlyList<DownloadCandidate> candidates,
        string articleTitle,
        bool isOnlyBook,
        string? pageIsbn)
    {
        for (var i = 0; i < decisions.Count; i++)
        {
            var candidate = candidates[i];
            var parsed = FeedImportAnalysisRules.ParseCandidate(candidate, articleTitle, isOnlyBook, pageIsbn);
            var hosts = string.Join(", ", candidate.Mirrors.Select(m => m.Host).Distinct(StringComparer.OrdinalIgnoreCase));
            var failed = decisions[i].FailWithLinks(
                [candidate], parsed, ExtractionStep, $"{FeedImportError.DownloadHostNotSupported.Description}: {hosts}.");
            if (failed.IsFailure)
            {
                return failed;
            }
        }

        return Result.Success();
    }

    private static Uri ToHttps(Uri uri) => uri.Scheme == Uri.UriSchemeHttp
        ? new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri
        : uri;
}
