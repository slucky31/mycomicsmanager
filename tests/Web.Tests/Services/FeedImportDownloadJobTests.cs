using System.Reflection;
using AwesomeAssertions;
using Hangfire;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class FeedImportDownloadJobTests
{
    [Fact]
    public void DownloadAsync_Should_RunOnTheDedicatedQueue_WhenEnqueued()
    {
        var method = typeof(FeedImportDownloadJob).GetMethod(nameof(FeedImportDownloadJob.DownloadAsync))!;

        method.GetCustomAttribute<QueueAttribute>()!.Queue.Should().Be(FeedImportDownloadJob.Queue);
    }
}
