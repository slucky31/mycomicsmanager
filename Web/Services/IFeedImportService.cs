using Application.FeedImports.Arbitrate;
using Application.FeedImports.Manage;
using Domain.FeedImports;
using Domain.Primitives;
using Web.Models;

namespace Web.Services;

public interface IFeedImportService
{
    bool IsSyncEnabled { get; }

    Task<Result<FeedImportDecisionPageViewModel>> GetDecisionsAsync(
        FeedImportDecisionStatus? status,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Result TriggerSync();

    Task<Result> ResolveArbitrationAsync(
        Guid decisionId,
        FeedImportArbitrationAction action,
        int? candidateIndex,
        CancellationToken cancellationToken = default);

    Task<Result> ApplyActionAsync(Guid decisionId, FeedImportDecisionAction action, CancellationToken cancellationToken = default);

    Task<Result> CorrectAsync(Guid decisionId, string serie, string? title, int? volume, CancellationToken cancellationToken = default);

    // Deletes each decision on its own: returns how many were deleted (the others are logged),
    // or the last error when none could be deleted.
    Task<Result<int>> DeleteAsync(IReadOnlyCollection<Guid> decisionIds, CancellationToken cancellationToken = default);

    Task<int> CountAwaitingArbitrationAsync(CancellationToken cancellationToken = default);
}
