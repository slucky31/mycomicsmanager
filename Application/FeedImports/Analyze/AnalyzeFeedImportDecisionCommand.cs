using Application.Abstractions.Messaging;

namespace Application.FeedImports.Analyze;

public record AnalyzeFeedImportDecisionCommand(Guid DecisionId) : ICommand;
