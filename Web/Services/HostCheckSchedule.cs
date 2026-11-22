using System.Globalization;
using Hangfire;
using Hangfire.Storage;
using Web.Models;

namespace Web.Services;

// When the Debrid-Link host states were last checked and will be next (Settings, Debrid-Link hosts): the
// host changes alone cannot tell it, since a check that finds no change records nothing.
public sealed record HostCheckTimes(DateTime? LastCheck, DateTime? NextCheck)
{
    public static HostCheckTimes Unknown { get; } = new(null, null);

    // "Last check 22/11/2026 11:20 · next at 11:30", "Not checked yet".
    public string Display
    {
        get
        {
            var last = LastCheck is { } lastCheck ? $"Last check {FeedImportDecisionViewModel.FormatDate(lastCheck)}" : "Not checked yet";
            return NextCheck is { } nextCheck
                ? $"{last} · next at {nextCheck.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)}"
                : last;
        }
    }
}

public interface IHostCheckSchedule
{
    HostCheckTimes GetCheckTimes();
}

// Reads the recurring job of HostStatusJob in the Hangfire storage.
public sealed class HostCheckSchedule(JobStorage storage) : IHostCheckSchedule
{
    public HostCheckTimes GetCheckTimes()
    {
        using var connection = storage.GetConnection();
        var job = connection.GetRecurringJobs([HostStatusJob.RecurringJobId]).FirstOrDefault(j => !j.Removed);
        return job is null ? HostCheckTimes.Unknown : new HostCheckTimes(job.LastExecution, job.NextExecution);
    }
}
