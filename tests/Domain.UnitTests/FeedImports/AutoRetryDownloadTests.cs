using Domain.FeedImports;

namespace Domain.UnitTests.FeedImports;

public class AutoRetryDownloadTests
{
    private static readonly DateTime s_downloadedAt = new(2026, 11, 22, 9, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static FeedImportDecision CreateFailedOnHostDown()
    {
        var decision = FeedImportDecision.Create(Guid.CreateVersion7(), 42, "Blacksad - Tome 3", "https://planete-bd.org/blacksad-3", null).Value!;
        var candidate = new DownloadCandidate("Blacksad T03", "Blacksad T03.cbz", null, [new DownloadMirror("https://ddl.to/abc", "ddl.to")]);
        decision.RecordLinks([candidate], new ParsedComicTitle("Blacksad", null, 3), "1 mirror", FeedImportDecidedBy.Auto);
        decision.StartDownload();
        decision.Fail(FeedImportConstants.HostDownStep, "Host down on Debrid-Link. (ddl.to)");
        return decision;
    }

    [Fact]
    public void From_Should_ReturnNull_WhenTheDecisionWasNotRetriedAutomatically()
    {
        AutoRetryDownload.From(CreateFailedOnHostDown()).Should().BeNull();
    }

    [Fact]
    public void From_Should_RecordTheDecisionAndItsRetries_WhenTheDecisionWasRetriedAutomatically()
    {
        var decision = CreateFailedOnHostDown();
        decision.RetryAutomatically(_ => true);

        var download = AutoRetryDownload.From(decision, new FixedTimeProvider(s_downloadedAt));

        download.Should().NotBeNull();
        download.Id.Should().NotBeEmpty();
        download.UserId.Should().Be(decision.UserId);
        download.DecisionId.Should().Be(decision.Id);
        download.AutoRetryCount.Should().Be(1);
        download.DownloadedAt.Should().Be(s_downloadedAt);
    }

    [Fact]
    public void From_Should_Throw_WhenTheDecisionIsNull()
    {
        var act = () => AutoRetryDownload.From(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
