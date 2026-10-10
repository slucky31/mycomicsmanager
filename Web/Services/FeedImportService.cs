using Application.FeedImports;
using Application.FeedImports.Arbitrate;
using Application.FeedImports.Delete;
using Application.FeedImports.List;
using Application.FeedImports.Manage;
using Application.Interfaces;
using Application.Libraries;
using Domain.FeedImports;
using Domain.Primitives;
using Domain.Settings;
using Hangfire;
using Microsoft.Extensions.Options;
using Web.Models;

namespace Web.Services;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107", Justification = "Distinct collaborators; the handlers are already grouped in FeedImportHandlers, bundling the rest would only hide them.")]
public class FeedImportService(
    FeedImportHandlers handlers,
    ICurrentUserService currentUserService,
    ILibraryReadService libraryReadService,
    IFeedImportDecisionReadService decisionReadService,
    IBackgroundJobClient backgroundJobClient,
    IOptions<FeedImportSettings> feedImportSettings,
    IFeatureToggles featureToggles,
    IOptions<DebridLinkSettings> debridLinkSettings,
    ILogger<FeedImportService> logger) : IFeedImportService
{
    public bool IsSyncEnabled => featureToggles.IsEnabled(FeatureToggle.FeedImport);

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
        var result = await handlers.GetDecisions.Handle(query, cancellationToken);
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

        var result = await handlers.ResolveArbitration.Handle(
            new ResolveFeedImportArbitrationCommand(decisionId, userIdResult.Value, action, candidateIndex), cancellationToken);
        if (result.IsSuccess)
        {
            // Resolved towards a download: start it now (the job does nothing if the decision is not LinksExtracted).
            EnqueueDownload(decisionId);
        }

        return result;
    }

    public async Task<Result> ApplyActionAsync(Guid decisionId, FeedImportDecisionAction action, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var result = await handlers.Manage.Handle(new ManageFeedImportDecisionCommand(decisionId, userIdResult.Value, action), cancellationToken);
        return StartNextStep(decisionId, result);
    }

    public async Task<Result> CorrectAsync(Guid decisionId, string serie, string? title, int? volume, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var result = await handlers.Correct.Handle(
            new CorrectFeedImportDecisionCommand(decisionId, userIdResult.Value, serie, title, volume), cancellationToken);
        return StartNextStep(decisionId, result);
    }

    public async Task<Result<int>> DeleteAsync(IReadOnlyCollection<Guid> decisionIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decisionIds);
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var deleted = 0;
        TError? lastError = null;
        foreach (var decisionId in decisionIds)
        {
            var result = await handlers.Delete.Handle(new DeleteFeedImportDecisionCommand(decisionId, userIdResult.Value), cancellationToken);
            if (result.IsSuccess)
            {
                deleted++;
            }
            else
            {
                lastError = result.Error;
                logger.LogWarning("Feed import decision {DecisionId} not deleted: [{Code}] {Description}",
                    decisionId, result.Error!.Code, result.Error.Description);
            }
        }

        // Nothing deleted: the reason is shown to the user.
        return deleted == 0 && lastError is not null ? lastError : deleted;
    }

    // Badge of the navigation bar: 0 when the user cannot be resolved (not signed in yet).
    public async Task<int> CountAwaitingArbitrationAsync(CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        return userIdResult.IsFailure
            ? 0
            : await decisionReadService.CountByStatusAsync(userIdResult.Value, FeedImportDecisionStatus.AwaitingArbitration, cancellationToken);
    }

    private Result StartNextStep(Guid decisionId, Result<FeedImportDecisionStatus> result)
    {
        if (result.IsFailure)
        {
            return result.Error!;
        }

        if (result.Value == FeedImportDecisionStatus.LinksExtracted)
        {
            EnqueueDownload(decisionId);
        }
        else if (result.Value == FeedImportDecisionStatus.Pending && IsSyncEnabled)
        {
            // No link yet: the article is analyzed again by the sync.
            backgroundJobClient.Enqueue<FeedImportSyncJob>(job => job.SyncAsync(CancellationToken.None));
        }

        return Result.Success();
    }

    private void EnqueueDownload(Guid decisionId)
    {
        if (string.IsNullOrWhiteSpace(debridLinkSettings.Value.ApiKey))
        {
            logger.LogInformation("Feed import download of decision {DecisionId} not started: DebridLink:ApiKey is not set.", decisionId);
            return;
        }

        backgroundJobClient.Enqueue<FeedImportDownloadJob>(job => job.DownloadAsync(decisionId, CancellationToken.None));
    }
}
