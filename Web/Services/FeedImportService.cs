using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.List;
using Application.Interfaces;
using Domain.FeedImports;
using Domain.Primitives;
using Hangfire;
using Microsoft.Extensions.Options;
using Web.Models;

namespace Web.Services;

public class FeedImportService(
    IQueryHandler<GetPagedFeedImportDecisionsQuery, IPagedList<FeedImportDecision>> getDecisionsHandler,
    ICurrentUserService currentUserService,
    IBackgroundJobClient backgroundJobClient,
    IOptions<FeedImportSettings> feedImportSettings) : IFeedImportService
{
    public bool IsSyncEnabled => feedImportSettings.Value.Enabled;

    public async Task<Result<FeedImportDecisionPageViewModel>> GetDecisionsAsync(
        FeedImportDecisionStatus? status,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new GetPagedFeedImportDecisionsQuery(userIdResult.Value, status, searchTerm, page, pageSize);
        var result = await getDecisionsHandler.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error!;
        }

        var items = (result.Value!.Items ?? []).Select(FeedImportDecisionViewModel.From).ToList();
        return new FeedImportDecisionPageViewModel(items, result.Value.TotalCount);
    }

    public Result TriggerSync()
    {
        if (!IsSyncEnabled)
        {
            return FeedImportError.Disabled;
        }

        backgroundJobClient.Enqueue<FeedImportSyncJob>(job => job.SyncAsync(CancellationToken.None));
        return Result.Success();
    }
}
