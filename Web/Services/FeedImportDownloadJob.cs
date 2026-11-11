using Application.Abstractions.Messaging;
using Application.FeedImports.Download;
using Hangfire;

namespace Web.Services;

// One Hangfire job per decision: a failure only affects its own book. The downloads have their own queue and
// Hangfire server (one worker), so a long or stuck download never blocks the sync or the imports.
public class FeedImportDownloadJob(ICommandHandler<DownloadFeedImportDecisionCommand> handler, ILogger<FeedImportDownloadJob> logger)
{
    public const string Queue = "feed_download";

    [Queue(Queue)]
    [AutomaticRetry(Attempts = 0)]
    [JobDisplayName("Feed import download {0}")]
    public async Task DownloadAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(new DownloadFeedImportDecisionCommand(decisionId), cancellationToken);
        if (result.IsFailure)
        {
            logger.LogError("Feed import download of decision {DecisionId} failed: [{Code}] {Description}",
                decisionId, result.Error!.Code, result.Error.Description);
        }
    }
}
