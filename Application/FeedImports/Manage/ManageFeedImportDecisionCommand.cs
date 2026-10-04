using Application.Abstractions.Messaging;
using Domain.FeedImports;

namespace Application.FeedImports.Manage;

// Returns the new status: the caller starts the download (LinksExtracted) or the analysis (Pending).
public record ManageFeedImportDecisionCommand(Guid DecisionId, Guid UserId, FeedImportDecisionAction Action)
    : ICommand<FeedImportDecisionStatus>;
