using Application.Abstractions.Messaging;

namespace Application.FeedImports.Download;

public record DownloadFeedImportDecisionCommand(Guid DecisionId) : ICommand;
