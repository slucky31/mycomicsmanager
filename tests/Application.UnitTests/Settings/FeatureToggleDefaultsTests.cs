using Domain.Settings;

namespace Application.UnitTests.Settings;

public class FeatureToggleDefaultsTests
{
    private readonly FeatureToggleTestData _data = new();

    [Fact]
    public void IsConfiguredOn_Should_ReturnTheConfiguredValueOfEachFeature()
    {
        _data.Bedetheque = FeatureToggleTestData.CreateBedetheque(enabled: false);
        var defaults = _data.CreateDefaults();

        defaults.IsConfiguredOn(FeatureToggle.FeedImport).Should().BeFalse();
        defaults.IsConfiguredOn(FeatureToggle.IsbnOcr).Should().BeTrue();
        defaults.IsConfiguredOn(FeatureToggle.Bedetheque).Should().BeFalse();
        defaults.IsConfiguredOn((FeatureToggle)99).Should().BeFalse();
    }

    [Fact]
    public void GetMissingConfiguration_Should_ReturnNull_WhenEveryFeatureIsFullyConfigured()
    {
        var defaults = _data.CreateDefaults();

        foreach (var toggle in Enum.GetValues<FeatureToggle>())
        {
            defaults.GetMissingConfiguration(toggle).Should().BeNull();
        }
    }

    [Fact]
    public void GetMissingConfiguration_Should_RequireUserEmail_WhenFeedImportHasNone()
    {
        _data.FeedImport.UserEmail = " ";

        _data.CreateDefaults().GetMissingConfiguration(FeatureToggle.FeedImport).Should().Be("FeedImport:UserEmail is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ftp://miniflux")]
    public void GetMissingConfiguration_Should_RequireHttpMinifluxUrl_WhenBaseUrlIsInvalid(string? address)
    {
        _data.Miniflux.BaseUrl = address is null ? null : new Uri(address);

        _data.CreateDefaults().GetMissingConfiguration(FeatureToggle.FeedImport).Should().Be("Miniflux:BaseUrl must be an absolute http(s) URL.");
    }

    [Fact]
    public void GetMissingConfiguration_Should_RequireMinifluxApiKey_WhenItIsMissing()
    {
        _data.Miniflux.ApiKey = string.Empty;

        _data.CreateDefaults().GetMissingConfiguration(FeatureToggle.FeedImport).Should().Be("Miniflux:ApiKey is required.");
    }

    [Fact]
    public void GetMissingConfiguration_Should_RequireMinifluxCategory_WhenItIsMissing()
    {
        _data.Miniflux.CategoryName = string.Empty;

        _data.CreateDefaults().GetMissingConfiguration(FeatureToggle.FeedImport).Should().Be("Miniflux:CategoryName is required.");
    }

    [Fact]
    public void GetMissingConfiguration_Should_RequireTesseractPath_WhenIsbnOcrHasNone()
    {
        _data.IsbnOcr.TesseractPath = string.Empty;

        _data.CreateDefaults().GetMissingConfiguration(FeatureToggle.IsbnOcr).Should().Be("IsbnOcr:TesseractPath is required.");
    }
}
