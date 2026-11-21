using Application.Abstractions.Messaging;
using Application.FeedImports.Arbitrate;
using Application.FeedImports.Delete;
using Application.FeedImports.HostStatus;
using Application.FeedImports.List;
using Application.FeedImports.Manage;
using Application.FeedImports.Retry;
using Domain.FeedImports;

namespace Web.Services;

public sealed record FeedImportHandlers(
    IQueryHandler<GetPagedFeedImportDecisionsQuery, FeedImportDecisionPage> GetDecisions,
    ICommandHandler<ResolveFeedImportArbitrationCommand> ResolveArbitration,
    ICommandHandler<ManageFeedImportDecisionCommand, FeedImportDecisionStatus> Manage,
    ICommandHandler<CorrectFeedImportDecisionCommand, FeedImportDecisionStatus> Correct,
    ICommandHandler<DeleteFeedImportDecisionCommand> Delete,
    ICommandHandler<DeleteDownloadedFeedImportDecisionsCommand, DeleteDownloadedFeedImportDecisionsResult> DeleteDownloaded,
    ICommandHandler<RetryWaitingFeedImportDecisionsCommand, IReadOnlyList<RetriedFeedImportDecision>> RetryWaiting,
    IQueryHandler<CountRetryableFeedImportDecisionsQuery, int> CountRetryable,
    IQueryHandler<GetHostStatusOverviewQuery, HostStatusOverview> HostStatusOverview);
