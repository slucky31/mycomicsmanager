using Application.Abstractions.Messaging;
using Application.FeedImports.Analysis;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.Arbitrate;

public sealed class ResolveFeedImportArbitrationCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IBookReadService bookReadService,
    IUnitOfWork unitOfWork) : ICommandHandler<ResolveFeedImportArbitrationCommand>
{
    public async Task<Result> Handle(ResolveFeedImportArbitrationCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.DecisionId == Guid.Empty || command.UserId == Guid.Empty || !Enum.IsDefined(command.Action) ||
            (command.Action == FeedImportArbitrationAction.KeepCandidate && command.CandidateIndex is null or < 0))
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
            FeedImportArbitrationAction.NotDuplicate => decision.ConfirmNotDuplicate(),
            FeedImportArbitrationAction.ConfirmDuplicate => decision.ConfirmDuplicate(),
            _ => await ResolveLinksAsync(decision, command, cancellationToken)
        };
        if (result.IsFailure)
        {
            return result;
        }

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsFailure ? saveResult.Error! : Result.Success();
    }

    private async Task<Result> ResolveLinksAsync(FeedImportDecision decision, ResolveFeedImportArbitrationCommand command, CancellationToken cancellationToken)
    {
        if (decision.Status != FeedImportDecisionStatus.AwaitingArbitration ||
            decision.ArbitrationKind != FeedImportArbitrationKind.AmbiguousLinks)
        {
            return FeedImportError.InvalidStatusTransition;
        }

        var candidates = decision.GetCandidates();
        DownloadCandidate chosen;
        if (command.Action == FeedImportArbitrationAction.KeepCandidate)
        {
            if (command.CandidateIndex!.Value >= candidates.Count)
            {
                return FeedImportError.BadRequest;
            }
            chosen = candidates[command.CandidateIndex.Value];
        }
        else
        {
            chosen = new DownloadCandidate(
                decision.EntryTitle,
                candidates.Select(c => c.FileName).FirstOrDefault(f => f is not null),
                candidates.Select(c => c.SizeBytes).FirstOrDefault(s => s.HasValue),
                candidates.SelectMany(c => c.Mirrors).DistinctBy(m => m.Url, StringComparer.Ordinal).ToList());
        }

        var books = await bookReadService.ListIdentitiesByUserAsync(decision.UserId, cancellationToken);
        var parsed = FeedImportAnalysisRules.ParseCandidate(chosen, decision.EntryTitle, isOnlyBookOfArticle: true);
        return FeedImportAnalysisRules.ApplyDuplicateCheck(decision, chosen, parsed, books, FeedImportDecidedBy.User);
    }
}
