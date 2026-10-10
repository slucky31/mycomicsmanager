using Application.Abstractions.Messaging;

namespace Application.FeedImports.Delete;

public record DeleteDownloadedFeedImportDecisionsCommand(Guid UserId) : ICommand<DeleteDownloadedFeedImportDecisionsResult>;
