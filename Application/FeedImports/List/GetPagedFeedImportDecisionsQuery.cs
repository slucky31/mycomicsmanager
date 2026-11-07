using Application.Abstractions.Messaging;
using Domain.FeedImports;

namespace Application.FeedImports.List;

public record GetPagedFeedImportDecisionsQuery(
    Guid UserId,
    FeedImportDecisionStatus? Status,
    // A host of the links, FeedImportConstants.NoHost for the decisions without link, null for all.
    string? Host,
    string? SearchTerm,
    int Page,
    int PageSize) : IQuery<FeedImportDecisionPage>;
