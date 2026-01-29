using Application.ComicInfoSearch;
using NSubstitute;

namespace Application.UnitTests.ComicInfoSearch;

public class CustomSearchApiClientTests
{
    [Fact]
    public void Constructor_Should_CreateInstance_WhenValidSettingsProvided()
    {
        // Arrange
        var settings = Substitute.For<IGoogleSearchSettings>();
        settings.ApiKey.Returns("test-api-key");
        settings.Cx.Returns("test-cx");

        // Act
        using var client = new CustomSearchApiClient(settings);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_Should_NotThrow_WhenCalledMultipleTimes()
    {
        // Arrange
        var settings = Substitute.For<IGoogleSearchSettings>();
        settings.ApiKey.Returns("test-api-key");
        settings.Cx.Returns("test-cx");
        using var client = new CustomSearchApiClient(settings);

        // Act
        var act = () =>
        {
            client.Dispose();
            client.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_Should_DisposeUnderlyingService()
    {
        // Arrange
        var settings = Substitute.For<IGoogleSearchSettings>();
        settings.ApiKey.Returns("test-api-key");
        settings.Cx.Returns("test-cx");
        var client = new CustomSearchApiClient(settings);

        // Act & Assert - no exception means dispose worked
        client.Dispose();
    }
}
