using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Web.Models;

public enum HealthTone
{
    Healthy,
    Degraded,
    Unhealthy
}

public sealed record HealthCheckViewModel(string Name, string Status, HealthTone Tone);

/// <summary>
/// The system health summarised on one line ("6 of 6 healthy"), with the detail of each check.
/// </summary>
public sealed record SystemHealthViewModel(string Summary, HealthTone Tone, IReadOnlyList<HealthCheckViewModel> Checks)
{
    public static readonly SystemHealthViewModel Unavailable = new("Unavailable", HealthTone.Unhealthy, []);

    public static SystemHealthViewModel From(HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var checks = report.Entries
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .Select(e => new HealthCheckViewModel(e.Key, e.Value.Status.ToString(), ToneOf(e.Value.Status)))
            .ToList();

        if (checks.Count == 0)
        {
            return new SystemHealthViewModel("No checks", HealthTone.Healthy, checks);
        }

        var healthy = checks.Count(c => c.Tone == HealthTone.Healthy);
        var tone = checks.Max(c => c.Tone);
        return new SystemHealthViewModel($"{healthy} of {checks.Count} healthy", tone, checks);
    }

    public static string CssClass(HealthTone tone) => tone switch
    {
        HealthTone.Healthy => "health-healthy",
        HealthTone.Degraded => "health-degraded",
        _ => "health-unhealthy"
    };

    private static HealthTone ToneOf(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => HealthTone.Healthy,
        HealthStatus.Degraded => HealthTone.Degraded,
        _ => HealthTone.Unhealthy
    };
}
