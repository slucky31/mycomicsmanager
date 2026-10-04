using Application.Abstractions.Messaging;
using Application.FeedImports.Analysis;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.Manage;

public sealed class CorrectFeedImportDecisionCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IBookReadService bookReadService,
    IUnitOfWork unitOfWork) : ICommandHandler<CorrectFeedImportDecisionCommand, FeedImportDecisionStatus>
{
    public async Task<Result<FeedImportDecisionStatus>> Handle(CorrectFeedImportDecisionCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.DecisionId == Guid.Empty || command.UserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.Serie) ||
            command.Serie.Length > FeedImportConstants.MaxParsedSerieLength ||
            command.Title?.Length > FeedImportConstants.MaxParsedTitleLength ||
            command.Volume is < 0)
        {
            return FeedImportError.BadRequest;
        }

        var decision = await decisionRepository.GetByIdAsync(command.DecisionId, cancellationToken);
        if (decision is null || decision.UserId != command.UserId)
        {
            return FeedImportError.NotFound;
        }

        var parsed = new ParsedComicTitle(command.Serie.Trim(), string.IsNullOrWhiteSpace(command.Title) ? null : command.Title.Trim(), command.Volume);
        var corrected = decision.Correct(parsed);
        if (corrected.IsFailure)
        {
            return corrected.Error!;
        }

        var books = await bookReadService.ListIdentitiesByUserAsync(decision.UserId, cancellationToken);
        var applied = FeedImportAnalysisRules.Reapply(decision, books, FeedImportDecidedBy.User);
        if (applied.IsFailure)
        {
            return applied.Error!;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error! : decision.Status;
    }
}
