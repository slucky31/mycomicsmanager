using Application.Settings;
using Domain.Settings;

namespace Application.UnitTests.Settings;

public class FeatureTogglesTests
{
    private readonly FeatureToggles _toggles = new(new FeatureToggleTestData().CreateDefaults());

    [Fact]
    public void IsEnabled_Should_FollowTheConfiguration_WhenNoOverrideIsLoaded()
    {
        _toggles.IsEnabled(FeatureToggle.FeedImport).Should().BeFalse();
        _toggles.IsEnabled(FeatureToggle.IsbnOcr).Should().BeTrue();
    }

    [Fact]
    public void Load_Should_ReplaceTheOverrides()
    {
        _toggles.Apply(FeatureToggle.IsbnOcr, false);

        _toggles.Load([FeatureToggleOverride.Create(FeatureToggle.FeedImport, true)]);

        _toggles.IsEnabled(FeatureToggle.FeedImport).Should().BeTrue();
        _toggles.IsEnabled(FeatureToggle.IsbnOcr).Should().BeTrue();
    }

    [Fact]
    public void Apply_Should_FollowTheConfigurationAgain_WhenValueIsNull()
    {
        _toggles.Apply(FeatureToggle.IsbnOcr, false);
        _toggles.IsEnabled(FeatureToggle.IsbnOcr).Should().BeFalse();

        _toggles.Apply(FeatureToggle.IsbnOcr, null);

        _toggles.IsEnabled(FeatureToggle.IsbnOcr).Should().BeTrue();
    }

    [Fact]
    public void GetStates_Should_ListEveryFeature_WithItsConfiguredValueAndOverride()
    {
        _toggles.Apply(FeatureToggle.FeedImport, true);

        var states = _toggles.GetStates();

        states.Select(s => s.Toggle).Should().Equal(Enum.GetValues<FeatureToggle>());
        states.Single(s => s.Toggle == FeatureToggle.FeedImport).Should().Be(new FeatureToggleState(FeatureToggle.FeedImport, true, false));
        states.Single(s => s.Toggle == FeatureToggle.FeedImport).IsOverridden.Should().BeTrue();
        states.Single(s => s.Toggle == FeatureToggle.IsbnOcr).IsOverridden.Should().BeFalse();
    }
}
