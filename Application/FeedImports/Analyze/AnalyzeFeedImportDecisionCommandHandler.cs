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
        var links = extraction.Links;
        if (links.Count == 0)
        {
            return decision.Fail(ExtractionStep, "No link to an allowed host (FeedImport:AllowedDownloadHosts).");
        }

        var grouping = DownloadLinkGrouper.Group(links, decision.EntryTitle);
        if (grouping.IsAmbiguous)
        {
            var parsed = ComicTitleParser.Parse(decision.EntryTitle);
            return decision.RequestArbitration(
                FeedImportArbitrationKind.AmbiguousLinks, grouping.Candidates, parsed, matchedBookId: null,
                grouping.AmbiguityReason!, FeedImportDecidedBy.Auto);
        }

        var books = await bookReadService.ListIdentitiesByUserAsync(decision.UserId, cancellationToken);
        return ApplyToBooks(decision, grouping.Candidates, books, extraction.Isbn);
    }

    // One decision per book: the original keeps the first book, the others become sibling decisions.
    private Result ApplyToBooks(
        FeedImportDecision decision,
        IReadOnlyList<DownloadCandidate> candidates,
        IReadOnlyList<Books.BookIdentityDto> books,
        string? pageIsbn)
    {
        var isOnlyBook = candidates.Count == 1;
        var decisions = new List<FeedImportDecision> { decision };
        for (var i = 1; i < candidates.Count; i++)
        {
            var sibling = decision.CreateSibling(i);
            if (sibling.IsFailure)
            {
                return sibling.Error!;
            }
            decisionRepository.Add(sibling.Value!);
            decisions.Add(sibling.Value!);
        }

        for (var i = 0; i < candidates.Count; i++)
        {
            // The ISBN printed on the page only describes the book when the article holds a single one.
            var parsed = FeedImportAnalysisRules.ParseCandidate(candidates[i], decision.EntryTitle, isOnlyBook, isOnlyBook ? pageIsbn : null);
            var applied = FeedImportAnalysisRules.ApplyDuplicateCheck(decisions[i], candidates[i], parsed, books, FeedImportDecidedBy.Auto);
            if (applied.IsFailure)
            {
                return applied;
            }
        }

        return Result.Success();
    }

    private static Uri ToHttps(Uri uri) => uri.Scheme == Uri.UriSchemeHttp
        ? new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri
        : uri;
}
