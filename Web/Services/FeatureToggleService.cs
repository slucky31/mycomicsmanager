using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.Settings;
using Application.Settings.SetFeatureToggle;
using Domain.Primitives;
using Domain.Settings;
using Hangfire;
using Microsoft.Extensions.Options;
using Web.Models;

namespace Web.Services;

public class FeatureToggleService(
    ICommandHandler<SetFeatureToggleCommand> setHandler,
    FeatureToggles featureToggles,
    IRecurringJobManager recurringJobManager,
    IOptions<FeedImportSettings> feedImportSettings,
    ILogger<FeatureToggleService> logger) : IFeatureToggleService
{
    public IReadOnlyList<FeatureToggleViewModel> GetToggles() =>
        [.. featureToggles.GetStates().Select(FeatureToggleViewModel.From)];

    public async Task<Result> SetAsync(FeatureToggle toggle, bool enabled, CancellationToken cancellationToken = default)
    {
        var result = await setHandler.Handle(new SetFeatureToggleCommand(toggle, enabled), cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        logger.LogInformation("Feature {Toggle} turned {State} from the Settings page", toggle, enabled ? "on" : "off");

        // The Miniflux sync is a recurring Hangfire job: add or remove it right away.
        if (toggle == FeatureToggle.FeedImport)
        {
            FeedImportSyncJob.Schedule(recurringJobManager, feedImportSettings.Value, featureToggles.IsEnabled(FeatureToggle.FeedImport));
        }

        return result;
    }
}
