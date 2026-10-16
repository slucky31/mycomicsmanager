using Application.Interfaces;
using Domain.Settings;
using NSubstitute;

namespace Application.UnitTests.ComicInfoSearch;

internal static class FeatureTogglesStub
{
    // Every feature on, or every feature off.
    public static IFeatureToggles All(bool enabled = true)
    {
        var toggles = Substitute.For<IFeatureToggles>();
        toggles.IsEnabled(Arg.Any<FeatureToggle>()).Returns(enabled);
        return toggles;
    }
}
