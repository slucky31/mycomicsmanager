using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.Arbitrate;
using Application.FeedImports.List;
using Application.Interfaces;
using Application.Libraries;
using Domain.FeedImports;
using Domain.Primitives;
using Hangfire;
using Microsoft.Extensions.Options;
using Web.Models;

namespace Web.Services;

public class FeedImportService(
    IQueryHandler<GetPagedFeedImportDecisionsQuery, FeedImportDecisionPage> getDecisionsHandler,
    ICommandHandler<ResolveFeedImportArbitrationCommand> resolveArbitrationHandler,
    ICurrentUserService currentUserService,
    ILibraryReadService libraryReadService,
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

        var decisionPage = result.Value!;
        var decisions = decisionPage.Decisions.Items ?? [];

        // Downloaded decisions link to the Import page, filtered on the library the files were deposited in.
        Guid? importLibraryId = null;
        if (decisions.Any(d => d.Status == FeedImportDecisionStatus.Downloaded))
        {
            var library = await libraryReadService.GetByNameAsync(feedImportSettings.Value.TargetLibraryName, userIdResult.Value, cancellationToken);
            importLibraryId = library?.Id;
        }

        var items = decisions
            .Select(d => FeedImportDecisionViewModel.From(d, decisionPage.MultiBookEntryIds.Contains(d.MinifluxEntryId), importLibraryId))
            .ToList();
        return new FeedImportDecisionPageViewModel(items, decisionPage.Decisions.TotalCount);
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

    public async Task<Result> ResolveArbitrationAsync(
        Guid decisionId,
        FeedImportArbitrationAction action,
        int? candidateIndex,
        CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await resolveArbitrationHandler.Handle(
            new ResolveFeedImportArbitrationCommand(decisionId, userIdResult.Value, action, candidateIndex), cancellationToken);
    }
}
