using Application.Abstractions.Messaging;
using Application.FeedImports.Arbitrate;
using Application.FeedImports.List;
using Application.FeedImports.Manage;
using Domain.FeedImports;

namespace Web.Services;

public sealed record FeedImportHandlers(
    IQueryHandler<GetPagedFeedImportDecisionsQuery, FeedImportDecisionPage> GetDecisions,
    ICommandHandler<ResolveFeedImportArbitrationCommand> ResolveArbitration,
    ICommandHandler<ManageFeedImportDecisionCommand, FeedImportDecisionStatus> Manage,
    ICommandHandler<CorrectFeedImportDecisionCommand, FeedImportDecisionStatus> Correct);
