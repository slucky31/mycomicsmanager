using System.Globalization;
using Application.FeedImports.HostStatus;
using Domain.FeedImports;
using Domain.Primitives;

namespace Web.Models;

// Settings, Debrid-Link hosts (#1318): the state of each host, then its changes grouped by day.
public sealed record HostStatusRowViewModel(string Name, bool IsUp, string StateDisplay, string? DomainsDisplay, string ChangesDisplay);

// PreviousDuration: how long the previous state lasted ("after 1 h 20 up"), null for the first reading of a host.
public sealed record HostStatusChangeViewModel(string Host, bool IsUp, string Text, string TimeDisplay, string? PreviousDuration);

public sealed record HostStatusDayViewModel(string Title, IReadOnlyList<HostStatusChangeViewModel> Changes);

public sealed record HostStatusPageViewModel(
    IReadOnlyList<HostStatusRowViewModel> Hosts,
    IReadOnlyList<HostStatusDayViewModel> Days,
    int DownCount,
    bool IsLive)
{
    public static HostStatusPageViewModel Empty { get; } = new([], [], 0, false);

    // downOnly: the "Down" chip; host: the history of one host (null: every host).
    public static HostStatusPageViewModel From(HostStatusOverview overview, bool downOnly = false, string? host = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(overview);

        var hosts = overview.Hosts
            .Where(h => !downOnly || !h.IsUp)
            .Select(h => new HostStatusRowViewModel(
                h.Name,
                h.IsUp,
                HostStates.Describe(h),
                DomainsDisplay(h.Domains),
                h.ChangesLast30Days switch
                {
                    0 => "no change in 30 days",
                    1 => "1 change in 30 days",
                    var n => string.Create(CultureInfo.InvariantCulture, $"{n} changes in 30 days"),
                }))
            .ToList();

        var today = Clock.UtcNow(timeProvider).ToLocalTime().Date;
        var days = overview.Changes
            .Where(c => host is null || string.Equals(c.Host, host, StringComparison.Ordinal))
            .Select(c => ToChange(c, overview.Changes))
            .GroupBy(c => c.Day)
            .OrderByDescending(g => g.Key)
            .Select(g => new HostStatusDayViewModel(DayTitle(g.Key, today), g.Select(c => c.Change).ToList()))
            .ToList();

        return new HostStatusPageViewModel(hosts, days, overview.Hosts.Count(h => !h.IsUp), overview.IsLive);
    }

    // changes: newest first, so the previous change of a host is the next one of the list with its name.
    private static (DateTime Day, HostStatusChangeViewModel Change) ToChange(HostStatusChange change, IReadOnlyList<HostStatusChange> changes)
    {
        var previous = changes.FirstOrDefault(p => p.Host == change.Host && p.ChangedAt < change.ChangedAt);
        var local = change.ChangedAt.ToLocalTime();
        string text;
        string? duration = null;
        if (previous is null)
        {
            text = change.IsUp ? "Up (first check)" : "Down (first check)";
        }
        else
        {
            text = change.IsUp ? "Back up" : "Went down";
            duration = $"after {FormatDuration(change.ChangedAt - previous.ChangedAt)} {(previous.IsUp ? "up" : "down")}";
        }

        return (local.Date, new HostStatusChangeViewModel(change.Host, change.IsUp, text, local.ToString("HH:mm", CultureInfo.InvariantCulture), duration));
    }

    private static string DayTitle(DateTime day, DateTime today)
    {
        if (day == today)
        {
            return "Today";
        }

        return day == today.AddDays(-1) ? "Yesterday" : day.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    // The first three domains: "rg.to, rapidgator.net, rapidgator.asia…".
    private static string? DomainsDisplay(IReadOnlyList<string> domains)
    {
        if (domains.Count == 0)
        {
            return null;
        }

        var shown = string.Join(", ", domains.Take(3));
        return domains.Count > 3 ? $"{shown}…" : shown;
    }

    // "35 min", "1 h 20", "3 h", "2 d 4 h".
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.FromHours(1))
        {
            return string.Create(CultureInfo.InvariantCulture, $"{Math.Max(1, (int)duration.TotalMinutes)} min");
        }

        if (duration < TimeSpan.FromDays(1))
        {
            var hours = string.Create(CultureInfo.InvariantCulture, $"{duration.Hours} h");
            return duration.Minutes == 0 ? hours : string.Create(CultureInfo.InvariantCulture, $"{hours} {duration.Minutes:00}");
        }

        var days = string.Create(CultureInfo.InvariantCulture, $"{duration.Days} d");
        return duration.Hours == 0 ? days : string.Create(CultureInfo.InvariantCulture, $"{days} {duration.Hours} h");
    }
}
