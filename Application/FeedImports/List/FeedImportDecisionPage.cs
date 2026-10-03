using Application.Interfaces;
using Domain.FeedImports;

namespace Application.FeedImports.List;

// MultiBookEntryIds: Miniflux entries of the page that were split into several books.
public sealed record FeedImportDecisionPage(IPagedList<FeedImportDecision> Decisions, IReadOnlySet<long> MultiBookEntryIds);
