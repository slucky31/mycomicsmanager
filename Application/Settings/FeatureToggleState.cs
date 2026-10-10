using Domain.Settings;

namespace Application.Settings;

public sealed record FeatureToggleState(FeatureToggle Toggle, bool Enabled, bool ConfiguredOn)
{
    public bool IsOverridden => Enabled != ConfiguredOn;
}
