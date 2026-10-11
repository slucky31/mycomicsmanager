using Domain.Settings;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Admin;

public partial class FeatureTogglesCard
{
    [Inject] private IFeatureToggleService FeatureToggleService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<FeatureTogglesCard> Logger { get; set; } = default!;

    private IReadOnlyList<FeatureToggleViewModel> _toggles = [];
    private bool _isSaving;

    protected override void OnInitialized() => _toggles = FeatureToggleService.GetToggles();

    internal async Task SetAsync(FeatureToggle toggle, bool enabled)
    {
        if (_isSaving)
        {
            return;
        }

        _isSaving = true;
        try
        {
            var result = await FeatureToggleService.SetAsync(toggle, enabled);
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to change the feature.", Severity.Error);
                Logger.LogError("Settings: feature {Toggle} not changed: {ErrorDescription}", toggle, result.Error?.Description);
            }
            else
            {
                Snackbar.Add($"{_toggles.First(t => t.Toggle == toggle).Label} turned {(enabled ? "on" : "off")}.", Severity.Success);
            }
        }
        finally
        {
            // Read back the actual state: on failure, the switch goes back to its previous value.
            _toggles = FeatureToggleService.GetToggles();
            _isSaving = false;
        }
    }
}
