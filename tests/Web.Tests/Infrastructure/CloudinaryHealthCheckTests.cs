using Application.Interfaces;
using AwesomeAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class CloudinaryHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Should_CacheResult_WhenCalledTwiceInQuickSuccession()
    {
        var cloudinaryService = Substitute.For<ICloudinaryService>();
        cloudinaryService.PingAsync(Arg.Any<CancellationToken>()).Returns(true);
        var sut = new CloudinaryHealthCheck(cloudinaryService);
        var context = new HealthCheckContext();

        await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);
        await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);

        await cloudinaryService.Received(1).PingAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_Should_ReturnUnhealthy_WhenCloudinaryUnreachable()
    {
        var cloudinaryService = Substitute.For<ICloudinaryService>();
        cloudinaryService.PingAsync(Arg.Any<CancellationToken>()).Returns(false);
        var sut = new CloudinaryHealthCheck(cloudinaryService);
        var context = new HealthCheckContext();

        var result = await sut.CheckHealthAsync(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
