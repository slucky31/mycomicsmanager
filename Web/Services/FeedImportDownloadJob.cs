using Application.Abstractions.Messaging;
using Application.FeedImports.Download;
using Hangfire;

namespace Web.Services;

// One Hangfire job per decision: a long download never blocks the sync, and a failure only affects its own book.
public class FeedImportDownloadJob(ICommandHandler<DownloadFeedImportDecisionCommand> handler)
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<FeedImportDownloadJob>();

    [AutomaticRetry(Attempts = 0)]
    [JobDisplayName("Feed import download {0}")]
    public async Task DownloadAsync(Guid decisionId, CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(new DownloadFeedImportDecisionCommand(decisionId), cancellationToken);
        if (result.IsFailure)
        {
            Log.Error("Feed import download of decision {DecisionId} failed: [{Code}] {Description}",
                decisionId, result.Error!.Code, result.Error.Description);
        }
    }
}
