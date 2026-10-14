using AwesomeAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class SystemHealthViewModelTests
{
    private static HealthReport Report(params (string Name, HealthStatus Status)[] entries) => new(
        entries.ToDictionary(e => e.Name, e => new HealthReportEntry(e.Status, null, TimeSpan.Zero, null, null)),
        TimeSpan.Zero);

    [Fact]
    public void From_Should_SummariseAndSortChecks_WhenAllHealthy()
    {
        var health = SystemHealthViewModel.From(Report(("postgres", HealthStatus.Healthy), ("cloudinary", HealthStatus.Healthy)));

        health.Summary.Should().Be("2 of 2 healthy");
        health.Tone.Should().Be(HealthTone.Healthy);
        health.Checks.Should().Equal(
            new HealthCheckViewModel("cloudinary", "Healthy", HealthTone.Healthy),
            new HealthCheckViewModel("postgres", "Healthy", HealthTone.Healthy));
    }

    [Theory]
    [InlineData(HealthStatus.Degraded, HealthTone.Degraded)]
    [InlineData(HealthStatus.Unhealthy, HealthTone.Unhealthy)]
    public void From_Should_TakeWorstTone_WhenOneCheckIsNotHealthy(HealthStatus status, HealthTone expected)
    {
        var health = SystemHealthViewModel.From(Report(("a", HealthStatus.Healthy), ("b", status), ("c", HealthStatus.Healthy)));

        health.Summary.Should().Be("2 of 3 healthy");
        health.Tone.Should().Be(expected);
        health.Checks[1].Should().Be(new HealthCheckViewModel("b", status.ToString(), expected));
    }

    [Fact]
    public void From_Should_SayNoChecks_WhenReportIsEmpty()
    {
        var health = SystemHealthViewModel.From(Report());

        health.Summary.Should().Be("No checks");
        health.Tone.Should().Be(HealthTone.Healthy);
        health.Checks.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HealthTone.Healthy, "health-healthy")]
    [InlineData(HealthTone.Degraded, "health-degraded")]
    [InlineData(HealthTone.Unhealthy, "health-unhealthy")]
    public void CssClass_Should_MatchTone(HealthTone tone, string expected)
    {
        SystemHealthViewModel.CssClass(tone).Should().Be(expected);
    }
}
