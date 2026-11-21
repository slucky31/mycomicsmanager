using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.Retry;

public sealed class CountRetryableFeedImportDecisionsQueryHandler(
    IFeedImportDecisionRepository decisionRepository,
    IDebridLinkClient debridLinkClient,
    TimeProvider timeProvider) : IQueryHandler<CountRetryableFeedImportDecisionsQuery, int>
{
    public async Task<Result<int>> Handle(CountRetryableFeedImportDecisionsQuery query, CancellationToken cancellationToken)
    {
        Guard.Against.Null(query);
        if (query.UserId == Guid.Empty)
        {
            return FeedImportError.BadRequest;
        }

        var (due, _) = await WaitingFeedImportDecisions.GetDueAsync(
            decisionRepository, debridLinkClient, timeProvider, query.UserId, FeedImportDecidedBy.User, cancellationToken);
        return due.Count;
    }
}
