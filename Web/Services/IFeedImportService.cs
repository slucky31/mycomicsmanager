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
}
