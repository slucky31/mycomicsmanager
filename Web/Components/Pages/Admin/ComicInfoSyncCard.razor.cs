using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Services;

namespace Web.Components.Pages.Admin;

public partial class ComicInfoSyncCard
{
    [Inject] private IComicInfoSyncService ComicInfoSyncService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<ComicInfoSyncCard> Logger { get; set; } = default!;

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
            var result = await ComicInfoSyncService.StartAsync();
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to update the ComicInfo.xml files.", Severity.Error);
                Logger.LogError("Settings: ComicInfo.xml sync not started: {ErrorDescription}", result.Error?.Description);
            }
            else if (result.Value == 0)
            {
                Snackbar.Add("No digital book to update.", Severity.Info);
            }
            else
            {
                Snackbar.Add($"{result.Value} book(s) will be updated in the background.", Severity.Success);
            }
        }
        finally
        {
            _isStarting = false;
        }
    }
}
