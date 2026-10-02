using System.Globalization;
using Application.Abstractions.Messaging;
using Application.FeedImports;
using Application.FeedImports.Sync;
using Application.Users;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Web.Services;

public class FeedImportSyncJob(IServiceScopeFactory scopeFactory, IOptions<FeedImportSettings> feedImportSettings)
{
    public const string RecurringJobId = "feed-import-sync";

    private const int MaxIntervalMinutes = 24 * 60;

    private static Serilog.ILogger Log => Serilog.Log.ForContext<FeedImportSyncJob>();

    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        var settings = feedImportSettings.Value;
        if (!settings.Enabled)
        {
            Log.Information("Feed import sync skipped: FeedImport:Enabled is false.");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var userReadService = scope.ServiceProvider.GetRequiredService<IUserReadService>();
        var userResult = await userReadService.GetUserByEmail(settings.UserEmail, cancellationToken);
        if (userResult.IsFailure)
        {
            Log.Error("Feed import sync aborted: user configured in FeedImport:UserEmail not found: [{Code}] {Description}",
                userResult.Error!.Code, userResult.Error.Description);
            return;
        }

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SyncFeedImportsCommand, SyncFeedImportsResult>>();
        var result = await handler.Handle(new SyncFeedImportsCommand(userResult.Value!.Id), cancellationToken);
        if (result.IsFailure)
        {
            Log.Error("Feed import sync failed: [{Code}] {Description}", result.Error!.Code, result.Error.Description);
            return;
        }

        var summary = result.Value!;
        Log.Information(
            "Feed import sync done: {Starred} starred entries, {Created} created, {AlreadyKnown} already known, {Rejected} rejected, {UnstarFailures} unstar failures.",
            summary.StarredEntries, summary.Created, summary.AlreadyKnown, summary.Rejected, summary.UnstarFailures);
    }

    public static bool IsValidInterval(int minutes) =>
        minutes is >= 1 and < 60 || (minutes >= 60 && minutes <= MaxIntervalMinutes && minutes % 60 == 0);

    public static string ToCron(int minutes) => minutes < 60
        ? string.Create(CultureInfo.InvariantCulture, $"*/{minutes} * * * *")
        : string.Create(CultureInfo.InvariantCulture, $"0 */{minutes / 60} * * *");

    public static void Schedule(IRecurringJobManager recurringJobManager, FeedImportSettings settings)
    {
        ArgumentNullException.ThrowIfNull(recurringJobManager);
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Enabled)
        {
            recurringJobManager.RemoveIfExists(RecurringJobId);
            return;
        }

        recurringJobManager.AddOrUpdate<FeedImportSyncJob>(
            RecurringJobId,
            job => job.SyncAsync(CancellationToken.None),
            ToCron(settings.SyncIntervalMinutes));
    }
}
