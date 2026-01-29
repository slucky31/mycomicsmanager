using Application.ComicInfoSearch;

namespace Application.UnitTests.ComicInfoSearch;

public class GoogleSearchSettingsTests
{
    [Fact]
    public void GoogleSearchSettings_Should_SetAndGetApiKey()
    {
        // Arrange & Act
        var settings = new GoogleSearchSettings
        {
            ApiKey = "test-api-key",
            Cx = "test-cx"
        };

        // Assert
        settings.ApiKey.Should().Be("test-api-key");
    }

    [Fact]
    public void GoogleSearchSettings_Should_SetAndGetCx()
    {
        // Arrange & Act
        var settings = new GoogleSearchSettings
        {
            ApiKey = "test-api-key",
            Cx = "test-cx"
        };

        // Assert
        settings.Cx.Should().Be("test-cx");
    }

    [Fact]
    public void GoogleSearchSettings_Should_ImplementIGoogleSearchSettings()
    {
        // Arrange & Act
        var settings = new GoogleSearchSettings
        {
            ApiKey = "test-api-key",
            Cx = "test-cx"
        };

        // Assert
        settings.Should().BeAssignableTo<IGoogleSearchSettings>();
    }
}
