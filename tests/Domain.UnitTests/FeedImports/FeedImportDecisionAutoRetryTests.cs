using Domain.FeedImports;

namespace Domain.UnitTests.FeedImports;

public class FeedImportDecisionAutoRetryTests
{
    private static readonly DateTime s_failedAt = new(2026, 11, 21, 18, 0, 0, DateTimeKind.Utc);

    private static readonly DownloadCandidate s_candidate = new(
        "Blacksad T03", "Blacksad T03.cbz", 50_000_000,
        [new DownloadMirror("https://ddl.to/abc", "ddl.to"), new DownloadMirror("https://1fichier.com/?abc", "1fichier.com")]);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static readonly Func<string, bool> s_fichierUp = host => host == "1fichier.com";
    private static readonly Func<string, bool> s_nothingUp = _ => false;

    private static FeedImportDecision CreateFailed(string step, string message)
    {
        var decision = FeedImportDecision.Create(Guid.CreateVersion7(), 42, "Blacksad - Tome 3", "https://planete-bd.org/blacksad-3", null).Value!;
        decision.RecordLinks([s_candidate], new ParsedComicTitle("Blacksad", null, 3), "2 mirrors", FeedImportDecidedBy.Auto);
        decision.StartDownload();
        decision.Fail(step, message, new FixedTimeProvider(s_failedAt));
        return decision;
    }

    [Theory]
    [InlineData("Host", "Host down on Debrid-Link. (ddl.to)", FeedImportRetryKind.HostDown)]
    [InlineData("Download", "Host not supported by Debrid-Link. (maintenanceHost)", FeedImportRetryKind.HostDown)]
    [InlineData("Download", "Debrid-Link could not be reached or returned an error.", FeedImportRetryKind.HostDown)]
    [InlineData("Download", "Debrid-Link quota reached.", FeedImportRetryKind.Quota)]
    [InlineData("Download", "Debrid-Link quota reached for this host.", FeedImportRetryKind.Quota)]
    [InlineData("Download", "File unavailable on the host (dead or deleted link).", FeedImportRetryKind.None)]
    [InlineData("Library", "Host not supported by Debrid-Link.", FeedImportRetryKind.None)]
    public void RetryKind_Should_ClassifyTheFailure_FromItsStepAndMessage(string step, string message, FeedImportRetryKind expected)
    {
        CreateFailed(step, message).RetryKind.Should().Be(expected);
    }

    [Fact]
    public void RetryKind_Should_BeNone_WhenTheDecisionIsNotFailed()
    {
        var decision = CreateFailed("Host", "Host down on Debrid-Link.");
        decision.Retry(s_failedAt);

        decision.RetryKind.Should().Be(FeedImportRetryKind.None);
    }

    [Fact]
    public void IsRetryDue_Should_DependOnTheHosts_WhenWaitingForAHost()
    {
        var decision = CreateFailed("Host", "Host down on Debrid-Link. (ddl.to, 1fichier.com)");

        decision.IsRetryDue(s_nothingUp, s_failedAt).Should().BeFalse();
        decision.IsRetryDue(s_fichierUp, s_failedAt).Should().BeTrue();
    }

    [Fact]
    public void IsRetryDue_Should_WaitForTheQuotaDelay_WhenTheQuotaWasReached()
    {
        var decision = CreateFailed("Download", "Debrid-Link quota reached.");

        decision.IsRetryDue(s_fichierUp, s_failedAt + FeedImportConstants.QuotaRetryDelay - TimeSpan.FromMinutes(1)).Should().BeFalse();
        decision.IsRetryDue(s_nothingUp, s_failedAt + FeedImportConstants.QuotaRetryDelay).Should().BeTrue();
    }

    [Fact]
    public void IsRetryDue_Should_BeFalse_WhenTheFailureIsReal()
    {
        CreateFailed("Download", "File too large (Import:MaxFileSizeMb).").IsRetryDue(s_fichierUp, s_failedAt.AddDays(2)).Should().BeFalse();
    }

    [Fact]
    public void RetryAutomatically_Should_ReopenAndCountTheRetry_WhenAHostIsBackUp()
    {
        var decision = CreateFailed("Host", "Host down on Debrid-Link. (ddl.to)");

        var result = decision.RetryAutomatically(s_fichierUp, new FixedTimeProvider(s_failedAt.AddHours(1)));

        result.IsSuccess.Should().BeTrue();
        decision.Status.Should().Be(FeedImportDecisionStatus.Pending);
        decision.DecidedBy.Should().Be(FeedImportDecidedBy.Auto);
        decision.AutoRetryCount.Should().Be(1);
        decision.ErrorStep.Should().BeNull();
        decision.Reason.Should().Be("Host back up on Debrid-Link: retried automatically (1/3).");
    }

    [Fact]
    public void RetryAutomatically_Should_SayTheQuotaPeriodIsOver_WhenRetryingAQuotaFailure()
    {
        var decision = CreateFailed("Download", "Debrid-Link quota reached.");

        decision.RetryAutomatically(s_nothingUp, new FixedTimeProvider(s_failedAt.AddDays(1)));

        decision.Reason.Should().Be("Debrid-Link quota period over: retried automatically (1/3).");
    }

    [Fact]
    public void RetryAutomatically_Should_ReturnInvalidTransition_WhenNotDue()
    {
        var decision = CreateFailed("Host", "Host down on Debrid-Link. (ddl.to)");

        decision.RetryAutomatically(s_nothingUp, new FixedTimeProvider(s_failedAt)).Error.Should().Be(FeedImportError.InvalidStatusTransition);
        decision.Status.Should().Be(FeedImportDecisionStatus.Failed);
    }

    [Fact]
    public void CanRetryAutomatically_Should_StopAfterTheMaximum_UntilTheUserRetries()
    {
        var decision = CreateFailed("Host", "Host down on Debrid-Link.");
        for (var i = 0; i < FeedImportConstants.MaxAutoRetries; i++)
        {
            decision.RetryAutomatically(s_fichierUp, new FixedTimeProvider(s_failedAt));
            decision.RecordLinks([s_candidate], new ParsedComicTitle("Blacksad", null, 3), "again", FeedImportDecidedBy.Auto);
            decision.StartDownload();
            decision.Fail("Host", "Host down on Debrid-Link.", new FixedTimeProvider(s_failedAt));
        }

        decision.CanRetryAutomatically(s_fichierUp, s_failedAt).Should().BeFalse();
        decision.IsRetryDue(s_fichierUp, s_failedAt).Should().BeTrue();

        decision.Retry(s_failedAt);
        decision.AutoRetryCount.Should().Be(0);
    }
}
