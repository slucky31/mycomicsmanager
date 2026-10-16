using Application.Settings;
using AwesomeAssertions;
using Domain.Settings;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class FeatureToggleViewModelTests
{
    [Theory]
    [InlineData(FeatureToggle.FeedImport, "Feed import", "FeedImport:Enabled")]
    [InlineData(FeatureToggle.IsbnOcr, "ISBN reading (OCR)", "IsbnOcr:Enabled")]
    [InlineData(FeatureToggle.Bedetheque, "Bedetheque search", "Bedetheque:Enabled")]
    [InlineData(FeatureToggle.Bnf, "BnF catalogue search", "BnfCatalogue:Enabled")]
    [InlineData(FeatureToggle.GoogleBooks, "Google Books search", "GoogleBooks:Enabled")]
    [InlineData(FeatureToggle.OpenLibrary, "OpenLibrary search", "OpenLibrary:Enabled")]
    [InlineData(FeatureToggle.Bubble, "Bubble search", "Bubble:Enabled")]
    [InlineData((FeatureToggle)99, "99", "99:Enabled")]
    public void From_Should_DescribeTheFeature(FeatureToggle toggle, string label, string configurationKey)
    {
        var viewModel = FeatureToggleViewModel.From(new FeatureToggleState(toggle, true, true));

        viewModel.Label.Should().Be(label);
        viewModel.ConfigurationKey.Should().Be(configurationKey);
    }

    [Fact]
    public void From_Should_ShowTheConfiguredValueAndOverride_WhenValueDiffersFromConfiguration()
    {
        var viewModel = FeatureToggleViewModel.From(new FeatureToggleState(FeatureToggle.FeedImport, true, false));

        viewModel.Enabled.Should().BeTrue();
        viewModel.IsOverridden.Should().BeTrue();
        viewModel.ConfigurationDisplay.Should().Be("Configured off (FeedImport:Enabled)");
    }
}
