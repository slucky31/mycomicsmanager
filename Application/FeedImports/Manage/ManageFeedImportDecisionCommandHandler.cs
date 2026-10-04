using Application.Abstractions.Messaging;
using Application.FeedImports.Analysis;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.Manage;

public sealed class ManageFeedImportDecisionCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IBookReadService bookReadService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<ManageFeedImportDecisionCommand, FeedImportDecisionStatus>
{
    public async Task<Result<FeedImportDecisionStatus>> Handle(ManageFeedImportDecisionCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.DecisionId == Guid.Empty || command.UserId == Guid.Empty || !Enum.IsDefined(command.Action))
        {
            return FeedImportError.BadRequest;
        }

        var decision = await decisionRepository.GetByIdAsync(command.DecisionId, cancellationToken);
        if (decision is null || decision.UserId != command.UserId)
        {
            return FeedImportError.NotFound;
        }

        var result = command.Action switch
        {
            FeedImportDecisionAction.ForceDownload => decision.ForceDownload(),
            FeedImportDecisionAction.Ignore => decision.Ignore(),
            _ => await RetryAsync(decision, cancellationToken)
        };
        if (result.IsFailure)
        {
            return result.Error!;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error! : decision.Status;
    }

    private async Task<Result> RetryAsync(FeedImportDecision decision, CancellationToken cancellationToken)
    {
        var retried = decision.Retry(timeProvider.GetUtcNow().UtcDateTime);
        if (retried.IsFailure)
        {
            return retried;
        }

        var books = await bookReadService.ListIdentitiesByUserAsync(decision.UserId, cancellationToken);
        return FeedImportAnalysisRules.Reapply(decision, books, FeedImportDecidedBy.User);
    }
}
