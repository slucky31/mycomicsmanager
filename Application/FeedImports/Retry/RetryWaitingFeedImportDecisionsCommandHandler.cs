using Application.Abstractions.Messaging;
using Application.FeedImports.Analysis;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Application.FeedImports.Retry;

public sealed class RetryWaitingFeedImportDecisionsCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IBookReadService bookReadService,
    IDebridLinkClient debridLinkClient,
    IUnitOfWork unitOfWork,
    ILogger<RetryWaitingFeedImportDecisionsCommandHandler> logger,
    TimeProvider timeProvider)
    : ICommandHandler<RetryWaitingFeedImportDecisionsCommand, IReadOnlyList<RetriedFeedImportDecision>>
{
    public async Task<Result<IReadOnlyList<RetriedFeedImportDecision>>> Handle(
        RetryWaitingFeedImportDecisionsCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.UserId == Guid.Empty || !Enum.IsDefined(command.DecidedBy))
        {
            return FeedImportError.BadRequest;
        }

        var (due, isHostUp) = await WaitingFeedImportDecisions.GetDueAsync(
            decisionRepository, debridLinkClient, timeProvider, command.UserId, command.DecidedBy, cancellationToken);
        if (due.Count == 0)
        {
            return Result.Success<IReadOnlyList<RetriedFeedImportDecision>>([]);
        }

        var books = await bookReadService.ListIdentitiesByUserAsync(command.UserId, cancellationToken);
        var retried = new List<RetriedFeedImportDecision>(due.Count);
        foreach (var decision in due)
        {
            var reopened = command.DecidedBy == FeedImportDecidedBy.Auto
                ? decision.RetryAutomatically(isHostUp, timeProvider)
                : decision.Retry(timeProvider.GetUtcNow().UtcDateTime);
            var reapplied = reopened.IsSuccess ? FeedImportAnalysisRules.Reapply(decision, books, command.DecidedBy, timeProvider) : reopened;
            if (reapplied.IsFailure)
            {
                logger.LogWarning("Feed import: decision {DecisionId} not retried: [{Code}] {Description}",
                    decision.Id, reapplied.Error!.Code, reapplied.Error.Description);
                continue;
            }

            retried.Add(new RetriedFeedImportDecision(decision.Id, decision.Status));
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error!;
        }

        logger.LogInformation("Feed import: {Count} decision(s) waiting for a host or the quota retried ({DecidedBy})", retried.Count, command.DecidedBy);
        return retried;
    }
}
