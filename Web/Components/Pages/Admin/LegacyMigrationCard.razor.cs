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
    private bool _isMoving;

    internal async Task MigrateAsync()
    {
        if (_isMoving)
        {
            return;
        }

        _isMoving = true;
        try
        {
            var result = await LegacyMigrationService.MigrateAsync(_count);
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to move the books.", Severity.Error);
                Logger.LogError("Settings: old library not migrated: {ErrorDescription}", result.Error?.Description);
            }
            else
            {
                var moved = result.Value!;
                Snackbar.Add($"{moved.Moved} book(s) moved, {moved.Skipped} skipped, {moved.Failed} failed. {moved.Remaining} left.",
                    moved.Failed > 0 ? Severity.Warning : Severity.Success);
            }
        }
        finally
        {
            _isMoving = false;
        }
    }
}
