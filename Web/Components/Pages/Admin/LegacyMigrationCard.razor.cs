using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Services;

namespace Web.Components.Pages.Admin;

public partial class LegacyMigrationCard
{
    [Inject] private ILegacyMigrationService LegacyMigrationService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<LegacyMigrationCard> Logger { get; set; } = default!;

    private int _count = 20;
    private bool _isStarting;

    internal async Task MigrateAsync()
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        try
        {
            var result = await LegacyMigrationService.StartAsync(_count);
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to move the books.", Severity.Error);
                Logger.LogError("Settings: old library not migrated: {ErrorDescription}", result.Error?.Description);
            }
            else
            {
                Snackbar.Add($"{result.Value} book(s) will be moved to the import folder in the background.", Severity.Success);
            }
        }
        finally
        {
            _isStarting = false;
        }
    }
}
