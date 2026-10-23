using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Services;

namespace Web.Components.Pages.Admin;

public partial class ArchiveRefreshCard
{
    [Inject] private IArchiveRefreshService ArchiveRefreshService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<ArchiveRefreshCard> Logger { get; set; } = default!;

    private bool _isStarting;

    internal async Task StartAsync()
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        try
        {
            var result = await ArchiveRefreshService.StartAsync();
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to refresh the cover and pages.", Severity.Error);
                Logger.LogError("Settings: cover and pages refresh not started: {ErrorDescription}", result.Error?.Description);
            }
            else if (result.Value == 0)
            {
                Snackbar.Add("No digital book to refresh.", Severity.Info);
            }
            else
            {
                Snackbar.Add($"{result.Value} book(s) will be refreshed in the background.", Severity.Success);
            }
        }
        finally
        {
            _isStarting = false;
        }
    }
}
