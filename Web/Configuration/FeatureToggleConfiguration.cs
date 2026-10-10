using Application.Interfaces;
using Application.Settings;

namespace Web.Configuration;

public static class FeatureToggleConfiguration
{
    // Loads the values chosen in the Settings page before anything reads a feature toggle.
    public static async Task LoadFeatureTogglesAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var overrides = await scope.ServiceProvider.GetRequiredService<IFeatureToggleOverrideRepository>().GetAllAsync();
        services.GetRequiredService<FeatureToggles>().Load(overrides);
    }
}
