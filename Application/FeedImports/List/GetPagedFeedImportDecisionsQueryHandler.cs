using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.List;

public sealed class GetPagedFeedImportDecisionsQueryHandler(IFeedImportDecisionReadService readService)
    : IQueryHandler<GetPagedFeedImportDecisionsQuery, IPagedList<FeedImportDecision>>
{
    public const int MaxPageSize = 100;

    public async Task<Result<IPagedList<FeedImportDecision>>> Handle(GetPagedFeedImportDecisionsQuery query, CancellationToken cancellationToken)
    {
        Guard.Against.Null(query);

        if (query.UserId == Guid.Empty ||
            query.Page < 1 ||
            query.PageSize < 1 ||
            query.PageSize > MaxPageSize ||
            (query.Status.HasValue && !Enum.IsDefined(query.Status.Value)))
        {
            return FeedImportError.BadRequest;
        }

        var decisions = await readService.GetPagedAsync(
            query.UserId, query.Status, query.SearchTerm?.Trim(), query.Page, query.PageSize, cancellationToken);

        return Result<IPagedList<FeedImportDecision>>.Success(decisions);
    }
}
