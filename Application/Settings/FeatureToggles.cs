using System.Collections.Concurrent;
using Application.Interfaces;
using Domain.Settings;

namespace Application.Settings;

// Registered as a singleton: the overrides are loaded from the database at startup, then kept
// up to date by SetFeatureToggleCommandHandler, so reading a toggle never hits the database.
// ponytail: one instance of the application; with several replicas, the others would miss a change.
public sealed class FeatureToggles(FeatureToggleDefaults defaults) : IFeatureToggles
{
    private readonly ConcurrentDictionary<FeatureToggle, bool> _overrides = new();

    public bool IsEnabled(FeatureToggle toggle) =>
        _overrides.TryGetValue(toggle, out var enabled) ? enabled : defaults.IsConfiguredOn(toggle);

    public IReadOnlyList<FeatureToggleState> GetStates() =>
        [.. Enum.GetValues<FeatureToggle>().Select(t => new FeatureToggleState(t, IsEnabled(t), defaults.IsConfiguredOn(t)))];

    public void Load(IEnumerable<FeatureToggleOverride> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);
        _overrides.Clear();
        foreach (var toggleOverride in overrides)
        {
            _overrides[toggleOverride.Toggle] = toggleOverride.Enabled;
        }
    }

    // null: the feature follows the configuration again.
    public void Apply(FeatureToggle toggle, bool? enabled)
    {
        if (enabled is { } value)
        {
            _overrides[toggle] = value;
        }
        else
        {
            _overrides.TryRemove(toggle, out _);
        }
    }
}
