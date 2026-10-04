using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.ImportJobs;
using Domain.Primitives;

namespace Application.FeedImports.Delete;

// Deletes one decision (its events go in cascade). The book imported from it is never touched.
// Each book of a multi-book article is its own decision: the others are kept.
// The Miniflux entry was unstarred by the sync, so it is not imported again unless starred again.
public sealed class DeleteFeedImportDecisionCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IImportJobRepository importJobRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<DeleteFeedImportDecisionCommand>
{
    public async Task<Result> Handle(DeleteFeedImportDecisionCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.DecisionId == Guid.Empty || command.UserId == Guid.Empty)
        {
            return FeedImportError.BadRequest;
        }

        var decision = await decisionRepository.GetByIdAsync(command.DecisionId, cancellationToken);
        if (decision is null || decision.UserId != command.UserId)
        {
            return FeedImportError.NotFound;
        }

        if (!decision.CanDelete(timeProvider.GetUtcNow().UtcDateTime) || await IsImportRunningAsync(decision, cancellationToken))
        {
            return FeedImportError.DeleteInProgress;
        }

        decisionRepository.Remove(decision);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error! : Result.Success();
    }

    // A deleted import job (removed from the Import page) no longer blocks the deletion.
    private async Task<bool> IsImportRunningAsync(FeedImportDecision decision, CancellationToken cancellationToken)
    {
        if (decision.ImportJobId is not { } importJobId)
        {
            return false;
        }

        var job = await importJobRepository.GetByIdAsync(importJobId, cancellationToken);
        return job is not null && job.Status is not (ImportJobStatus.Completed or ImportJobStatus.Failed);
    }
}
