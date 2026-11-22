using Application.FeedImports.HostStatus;
using Application.FeedImports.List;
using AwesomeAssertions;
using Domain.FeedImports;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class HostStatusViewModelsTests
{
    private static readonly DateTime s_now = new(2026, 11, 21, 23, 0, 0, DateTimeKind.Utc);
    private static readonly DownloadCandidate s_candidate = new("Blacksad T03", "Blacksad T03.cbz", null,
        [new DownloadMirror("https://ddl.to/a", "ddl.to"), new DownloadMirror("https://1fichier.com/?a", "1fichier.com")]);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static HostStatusChange Change(string host, bool isUp, DateTime at) =>
        HostStatusChange.Create(host, isUp, new FixedTimeProvider(at)).Value!;

    private static FeedImportDecision Failed(string step, string message)
    {
        var decision = FeedImportDecision.Create(Guid.CreateVersion7(), 12, "Blacksad T3", "https://planete-bd.org/12", null).Value!;
        decision.RecordLinks([s_candidate], new ParsedComicTitle("Blacksad", null, 3), "2 mirrors", FeedImportDecidedBy.Auto);
        decision.StartDownload();
        decision.Fail(step, message);
        return decision;
    }

    [Fact]
    public void From_Should_ShowWaitingForHost_WhenEveryMirrorIsOnAHostDown()
    {
        var viewModel = FeedImportDecisionViewModel.From(Failed("Host", "Host down on Debrid-Link. (ddl.to)"));

        viewModel.StatusDisplay.Should().Be(FeedImportDecisionViewModel.WaitingForHostDisplay);
        viewModel.StatusCssClass.Should().Be("feed-status-decide");
        viewModel.SummaryDisplay.Should().Be(
            "Host down on Debrid-Link. (ddl.to) Retried automatically once one of its hosts is back up on Debrid-Link (attempt 1 of 3).");
    }

    [Fact]
    public void From_Should_SayWhenAQuotaFailureIsRetried()
    {
        var viewModel = FeedImportDecisionViewModel.From(Failed("Download", "Debrid-Link quota reached."));

        viewModel.StatusDisplay.Should().Be("Failed");
        viewModel.SummaryDisplay.Should().Be("Download: Debrid-Link quota reached. Retried automatically 24 h after the failure (attempt 1 of 3).");
    }

    [Fact]
    public void From_Should_AskForAManualRetry_WhenTheAutomaticRetriesAreUsedUp()
    {
        var decision = Failed("Host", "Host down on Debrid-Link.");
        for (var i = 0; i < FeedImportConstants.MaxAutoRetries; i++)
        {
            decision.RetryAutomatically(_ => true);
            decision.RecordLinks([s_candidate], new ParsedComicTitle("Blacksad", null, 3), "again", FeedImportDecidedBy.Auto);
            decision.StartDownload();
            decision.Fail("Host", "Host down on Debrid-Link.");
        }

        FeedImportDecisionViewModel.From(decision).SummaryDisplay.Should().Be("Host down on Debrid-Link. Automatic retries used up: retry it by hand.");
    }

    [Fact]
    public void From_Should_GiveEachMirrorTheStateOfItsHost()
    {
        var viewModel = FeedImportDecisionViewModel.From(Failed("Host", "Host down on Debrid-Link."), hostState: host => host == "ddl.to" ? false : null);

        viewModel.Candidates.Single().Mirrors.Select(m => m.IsHostUp).Should().Equal(false, null);
    }

    [Fact]
    public void Describe_Should_SaySinceWhenTheHostIsInItsState()
    {
        var since = new DateTime(2026, 11, 21, 17, 2, 0, DateTimeKind.Utc);
        var date = FeedImportDecisionViewModel.FormatDate(since);

        HostStates.Describe(new HostStatusRow("ddl", false, [], since, false, 1)).Should().Be($"Down since {date}");
        HostStates.Describe(new HostStatusRow("ddl", true, [], since, true, 0)).Should().Be($"Up since at least {date}");
        HostStates.Describe(new HostStatusRow("ddl", true, [], null, false, 0)).Should().Be("Up");
    }

    [Fact]
    public void HostOptionsFrom_Should_AddTheStateOfTheKnownHosts()
    {
        var overview = new HostStatusOverview([new HostStatusRow("ddl", false, ["ddl.to"], null, false, 0)], [], true);

        var options = FeedImportHostOptions.From(
            new FeedImportHostCounts(3, [new FeedImportHostCount("ddl.to", 2), new FeedImportHostCount("turbobit.net", 1)], 0), overview);

        options.Options.Select(o => (o.Host, o.IsUp, o.StateDisplay)).Should().Equal(
            ((string?)null, (bool?)null, (string?)null), ("ddl.to", false, "Down"), ("turbobit.net", null, null));
    }

    [Fact]
    public void PageFrom_Should_ListTheHostsAndGroupTheChangesByDay()
    {
        var firstCheck = Change("ddl", true, s_now.AddDays(-3));
        var wentDown = Change("ddl", false, s_now.AddHours(-1));
        var overview = new HostStatusOverview(
            [
                new HostStatusRow("ddl", false, ["ddl.to", "ddownload.com", "a.b", "c.d"], wentDown.ChangedAt, false, 1),
                new HostStatusRow("1fichier", true, [], null, false, 2)
            ],
            [wentDown, firstCheck],
            true);

        var page = HostStatusPageViewModel.From(overview, timeProvider: new FixedTimeProvider(s_now));

        page.DownCount.Should().Be(1);
        page.Hosts.Select(h => (h.Name, h.DomainsDisplay, h.ChangesDisplay)).Should().Equal(
            ("ddl", "ddl.to, ddownload.com, a.b…", "1 change in 30 days"), ("1fichier", (string?)null, "2 changes in 30 days"));
        page.Days.Should().HaveCount(2);
        page.Days[0].Changes.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Host = "ddl",
            IsUp = false,
            Text = "Went down",
            PreviousDuration = "after 2 d 23 h up"
        });
        page.Days[1].Changes.Single().Text.Should().Be("Up (first check)");
        page.Days[1].Changes.Single().PreviousDuration.Should().BeNull();
    }

    [Fact]
    public void PageFrom_Should_KeepTheHostsDownAndTheHistoryOfOneHost_WhenFiltered()
    {
        var overview = new HostStatusOverview(
            [new HostStatusRow("ddl", false, [], null, false, 0), new HostStatusRow("1fichier", true, [], null, false, 0)],
            [Change("ddl", false, s_now), Change("1fichier", true, s_now)],
            true);

        var page = HostStatusPageViewModel.From(overview, downOnly: true, host: "1fichier", timeProvider: new FixedTimeProvider(s_now));

        page.Hosts.Select(h => h.Name).Should().Equal("ddl");
        page.Days.SelectMany(d => d.Changes).Select(c => c.Host).Should().Equal("1fichier");
        page.Hosts[0].ChangesDisplay.Should().Be("no change in 30 days");
    }

    [Theory]
    [InlineData(0, 0, 20, "1 min")]
    [InlineData(0, 0, 35 * 60, "35 min")]
    [InlineData(0, 3, 0, "3 h")]
    [InlineData(0, 1, 20 * 60, "1 h 20")]
    [InlineData(2, 0, 0, "2 d")]
    [InlineData(2, 4, 0, "2 d 4 h")]
    public void FormatDuration_Should_UseTheLargestUnits(int days, int hours, int seconds, string expected) =>
        HostStatusPageViewModel.FormatDuration(new TimeSpan(days, hours, 0, seconds)).Should().Be(expected);
}
