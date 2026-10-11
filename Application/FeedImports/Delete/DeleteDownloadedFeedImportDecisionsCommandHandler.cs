using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.ImportJobs;
using Domain.Primitives;

namespace Application.FeedImports.Delete;

// Deletes the Downloaded decisions whose import succeeded (or whose import job was removed from
// the Import page). The ones whose import is running or failed are kept, so a failure stays visible.
// The books imported from them are never touched.
public sealed class DeleteDownloadedFeedImportDecisionsCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IImportJobRepository importJobRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteDownloadedFeedImportDecisionsCommand, DeleteDownloadedFeedImportDecisionsResult>
{
    public async Task<Result<DeleteDownloadedFeedImportDecisionsResult>> Handle(
        DeleteDownloadedFeedImportDecisionsCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.UserId == Guid.Empty)
        {
            return FeedImportError.BadRequest;
        }

        var decisions = await decisionRepository.GetByStatusAsync(command.UserId, FeedImportDecisionStatus.Downloaded, cancellationToken);
        if (decisions.Count == 0)
        {
            return new DeleteDownloadedFeedImportDecisionsResult(0, 0);
        }

        var importJobIds = decisions.Where(d => d.ImportJobId is not null).Select(d => d.ImportJobId!.Value).Distinct().ToList();
        var importJobStatuses = (await importJobRepository.GetByIdsAsync(importJobIds, cancellationToken))
            .ToDictionary(j => j.Id, j => j.Status);

        var toDelete = decisions.Where(d => IsImportSucceededOrRemoved(d, importJobStatuses)).ToList();
        if (toDelete.Count == 0)
        {
            return new DeleteDownloadedFeedImportDecisionsResult(0, decisions.Count);
        }

        foreach (var decision in toDelete)
        {
            decisionRepository.Remove(decision);
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error!;
        }

        return new DeleteDownloadedFeedImportDecisionsResult(toDelete.Count, decisions.Count - toDelete.Count);
    }

    private static bool IsImportSucceededOrRemoved(FeedImportDecision decision, Dictionary<Guid, ImportJobStatus> importJobStatuses) =>
        decision.ImportJobId is not { } importJobId
        || !importJobStatuses.TryGetValue(importJobId, out var status)
        || status == ImportJobStatus.Completed;
}
