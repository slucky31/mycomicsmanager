using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.HostStatus;
using Application.Interfaces;
using Domain.Settings;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Web.Services;

// Every 10 minutes: records the Debrid-Link host state changes (Settings, Debrid-Link hosts) and, when a host is
// back up, starts a sync at once so the decisions waiting for it are retried without waiting for the next one.
public class HostStatusJob(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobClient backgroundJobClient,
    IFeatureToggles featureToggles,
    IOptions<DebridLinkSettings> debridLinkSettings,
    ILogger<HostStatusJob> logger)
{
    public const string RecurringJobId = "debrid-link-host-status";
    public const string Cron = "*/10 * * * *";

    [DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task RecordAsync(CancellationToken cancellationToken = default)
    {
        if (!featureToggles.IsEnabled(FeatureToggle.FeedImport))
        {
            return;
        }

        RecordHostStatusesResult recorded;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RecordHostStatusesCommand, RecordHostStatusesResult>>();
            var result = await handler.Handle(new RecordHostStatusesCommand(), cancellationToken);
            if (result.IsFailure)
            {
                logger.LogWarning("Debrid-Link host states not recorded: [{Code}] {Description}", result.Error!.Code, result.Error.Description);
                return;
            }

            recorded = result.Value!;
        }

        if (recorded.Changes > 0)
        {
            logger.LogInformation("Debrid-Link host states: {Changes} change(s), back up: {Hosts}",
                recorded.Changes, recorded.CameBackUp.Count == 0 ? "-" : string.Join(", ", recorded.CameBackUp));
        }

        if (recorded.CameBackUp.Count > 0 && !string.IsNullOrWhiteSpace(debridLinkSettings.Value.ApiKey))
        {
            backgroundJobClient.Enqueue<FeedImportSyncJob>(job => job.SyncAsync(CancellationToken.None));
        }
    }
}
