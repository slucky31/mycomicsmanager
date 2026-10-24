using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages;

public partial class UserSettings
{
    [Inject] private HealthCheckService HealthCheckService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<UserSettings> Logger { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private LibrariesNotifier LibrariesNotifier { get; set; } = default!;

    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _picture = string.Empty;

    private SystemHealthViewModel _health = SystemHealthViewModel.Unavailable;
    private bool _showHealthDetail;
    private bool _isLoadingHealth = true;

    private bool _showHiddenLibraries;
    private bool _isLoadingHidden = true;
    private bool _isSavingHidden;
    private int _hiddenVersion;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState is not null)
        {
            var state = await AuthenticationState;
            var user = state?.User;

            var rawPicture = user?.Claims
                .FirstOrDefault(c => c.Type.Equals("picture", StringComparison.Ordinal))?.Value
                ?? string.Empty;

            _picture = Uri.TryCreate(rawPicture, UriKind.Absolute, out var pictureUri)
                       && pictureUri.Scheme == Uri.UriSchemeHttps
                ? rawPicture
                : string.Empty;

            _username = user?.Claims
                .FirstOrDefault(c => c.Type.Equals("name", StringComparison.Ordinal))?.Value
                ?? string.Empty;

            _email = user?.FindFirstValue(ClaimTypes.Email)
                  ?? user?.FindFirstValue("email")
                  ?? string.Empty;
        }
        await LoadShowHiddenLibrariesAsync();
        await LoadHealthAsync();
        await base.OnInitializedAsync();
    }

    private async Task LoadShowHiddenLibrariesAsync()
    {
        try
        {
            var result = await LibrariesService.GetShowHiddenLibraries();
            if (result.IsSuccess)
            {
                _showHiddenLibraries = result.Value;
            }
            else if (result.IsFailure)
            {
                Snackbar.Add("Failed to load the setting of the hidden libraries", Severity.Error);
                Logger.LogError("Failed to load the setting of the hidden libraries: {Error}", result.Error?.Description);
            }
        }
        finally
        {
            _isLoadingHidden = false;
        }
    }

    // The setting is saved at once; the navigation lists the libraries again.
    private async Task SetShowHiddenLibrariesAsync(bool show)
    {
        _isSavingHidden = true;
        try
        {
            var result = await LibrariesService.SetShowHiddenLibraries(show);
            if (result.IsSuccess)
            {
                _showHiddenLibraries = show;
                LibrariesNotifier.NotifyChanged();
            }
            else
            {
                Snackbar.Add("Failed to save the setting of the hidden libraries", Severity.Error);
                Logger.LogError("Failed to save the setting of the hidden libraries: {Error}", result.Error?.Description);
            }
        }
        finally
        {
            _isSavingHidden = false;
            _hiddenVersion++;
        }
    }

    private async Task LoadHealthAsync()
    {
        _isLoadingHealth = true;
        try
        {
            _health = SystemHealthViewModel.From(await HealthCheckService.CheckHealthAsync());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Snackbar.Add("Failed to load system health", Severity.Error);
            Logger.LogError(ex, "Failed to load system health");
        }
        finally
        {
            _isLoadingHealth = false;
        }
    }

    private void ToggleHealthDetail() => _showHealthDetail = !_showHealthDetail;
}
