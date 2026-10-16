using AwesomeAssertions;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class LibraryIconsWarmupServiceTests
{
    [Fact]
    public async Task StartAsync_Should_Complete_AndSoShouldStopAsync()
    {
        var service = new LibraryIconsWarmupService();

        await service.StartAsync(TestContext.Current.CancellationToken);
        var stop = service.StopAsync(TestContext.Current.CancellationToken);

        stop.IsCompletedSuccessfully.Should().BeTrue();
    }
}
