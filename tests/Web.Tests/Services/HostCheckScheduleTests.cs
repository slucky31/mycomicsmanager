using AwesomeAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using NSubstitute;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public class HostCheckScheduleTests
{
    private static readonly DateTime s_lastCheck = new(2026, 11, 22, 10, 20, 0, DateTimeKind.Utc);
    private static readonly DateTime s_nextCheck = new(2026, 11, 22, 10, 30, 0, DateTimeKind.Utc);

    private readonly IStorageConnection _connection = Substitute.For<IStorageConnection>();
    private readonly HostCheckSchedule _schedule;

    public HostCheckScheduleTests()
    {
        var storage = Substitute.For<JobStorage>();
        storage.GetConnection().Returns(_connection);
        _schedule = new HostCheckSchedule(storage);
    }

    [Fact]
    public void GetCheckTimes_Should_ReturnTheLastAndNextRuns_WhenTheJobIsScheduled()
    {
        _connection.GetAllEntriesFromHash($"recurring-job:{HostStatusJob.RecurringJobId}").Returns(new Dictionary<string, string>
        {
            ["Cron"] = HostStatusJob.Cron,
            ["LastExecution"] = JobHelper.SerializeDateTime(s_lastCheck),
            ["NextExecution"] = JobHelper.SerializeDateTime(s_nextCheck),
        });

        var times = _schedule.GetCheckTimes();

        times.Should().Be(new HostCheckTimes(s_lastCheck, s_nextCheck));
        _connection.Received(1).Dispose();
    }

    [Fact]
    public void GetCheckTimes_Should_ReturnUnknown_WhenTheJobIsNotScheduled()
    {
        _connection.GetAllEntriesFromHash(Arg.Any<string>()).Returns((Dictionary<string, string>?)null);

        _schedule.GetCheckTimes().Should().Be(HostCheckTimes.Unknown);
    }

    [Fact]
    public void Display_Should_GiveTheLastCheckAndTheNextTime()
    {
        var next = s_nextCheck.ToLocalTime().ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        new HostCheckTimes(s_lastCheck, s_nextCheck).Display
            .Should().Be($"Last check {FeedImportDecisionViewModel.FormatDate(s_lastCheck)} · next at {next}");
        new HostCheckTimes(s_lastCheck, null).Display.Should().Be($"Last check {FeedImportDecisionViewModel.FormatDate(s_lastCheck)}");
        HostCheckTimes.Unknown.Display.Should().Be("Not checked yet");
    }
}
