using Application.Abstractions.Messaging;
using Domain.FeedImports;

namespace Application.FeedImports.Manage;

// New serie / title / volume, then the duplicate check runs again. Returns the new status.
public record CorrectFeedImportDecisionCommand(Guid DecisionId, Guid UserId, string Serie, string? Title, int? Volume)
    : ICommand<FeedImportDecisionStatus>;
