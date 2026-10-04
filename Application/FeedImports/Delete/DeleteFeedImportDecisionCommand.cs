using Application.Abstractions.Messaging;

namespace Application.FeedImports.Delete;

public record DeleteFeedImportDecisionCommand(Guid DecisionId, Guid UserId) : ICommand;
