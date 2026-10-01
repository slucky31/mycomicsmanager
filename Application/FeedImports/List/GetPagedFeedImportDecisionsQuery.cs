using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.FeedImports;

namespace Application.FeedImports.List;

public record GetPagedFeedImportDecisionsQuery(
    Guid UserId,
    FeedImportDecisionStatus? Status,
    string? SearchTerm,
    int Page,
    int PageSize) : IQuery<IPagedList<FeedImportDecision>>;
