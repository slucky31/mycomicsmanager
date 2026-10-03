using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.ImportJobs;
using Domain.Primitives;

namespace Application.FeedImports.Refresh;

public sealed class RefreshFeedImportStatusesCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IImportJobRepository importJobRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RefreshFeedImportStatusesCommand>
{
    public const string ImportStep = "Import";

    public async Task<Result> Handle(RefreshFeedImportStatusesCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.UserId == Guid.Empty)
        {
            return FeedImportError.BadRequest;
        }

        var decisions = await decisionRepository.GetByStatusAsync(command.UserId, FeedImportDecisionStatus.Downloaded, cancellationToken);
        var changed = false;
        foreach (var decision in decisions)
        {
            var job = decision.ImportJobId is { } jobId
                ? await importJobRepository.GetByIdAsync(jobId, cancellationToken)
                : null;
            changed |= Apply(decision, job);
        }

        if (!changed)
        {
            return Result.Success();
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error! : Result.Success();
    }

    private static bool Apply(FeedImportDecision decision, ImportJob? job)
    {
        if (job is null)
        {
            return decision.Fail(ImportStep, "La tâche d'import est introuvable (supprimée ?).").IsSuccess;
        }

        return job.Status switch
        {
            ImportJobStatus.Completed when job.DigitalBookId is { } bookId => decision.MarkImported(bookId).IsSuccess,
            ImportJobStatus.Completed => decision.Fail(ImportStep, "Import terminé sans livre associé.").IsSuccess,
            ImportJobStatus.Failed => decision.Fail(ImportStep, FormatImportError(job)).IsSuccess,
            _ => false
        };
    }

    private static string FormatImportError(ImportJob job)
    {
        if (string.IsNullOrWhiteSpace(job.ErrorMessage))
        {
            return "L'import a échoué.";
        }

        return string.IsNullOrWhiteSpace(job.ErrorStep)
            ? $"L'import a échoué : {job.ErrorMessage}"
            : $"L'import a échoué ({job.ErrorStep}) : {job.ErrorMessage}";
    }
}
