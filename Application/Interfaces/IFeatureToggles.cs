using Domain.Settings;

namespace Application.Interfaces;

// Whether a feature is on: the value chosen in the Settings page, else the configuration.
public interface IFeatureToggles
{
    bool IsEnabled(FeatureToggle toggle);
}
