using Domain.Primitives;

namespace Domain.Settings;

// Value chosen in the Settings page for a feature: it replaces the value of the configuration
// (appsettings / environment variables) until it is removed.
public sealed class FeatureToggleOverride : Entity<Guid>
{
    public FeatureToggle Toggle { get; private set; }

    public bool Enabled { get; private set; }

    private FeatureToggleOverride() { }

    public static FeatureToggleOverride Create(FeatureToggle toggle, bool enabled) => new()
    {
        Id = Guid.CreateVersion7(),
        Toggle = toggle,
        Enabled = enabled,
        CreatedOnUtc = DateTime.UtcNow
    };

    public void Set(bool enabled) => Enabled = enabled;
}
