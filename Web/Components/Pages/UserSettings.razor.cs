using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MudBlazor;
using Serilog;

namespace Web.Components.Pages;

public partial class UserSettings
{
    [Inject] private HealthCheckService HealthCheckService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _picture = string.Empty;

    private HealthReport? _healthReport;
    private bool _isLoadingHealth = true;

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
        await LoadHealthAsync();
        await base.OnInitializedAsync();
    }

    private async Task LoadHealthAsync()
    {
        _isLoadingHealth = true;
        try
        {
            _healthReport = await HealthCheckService.CheckHealthAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Snackbar.Add("Failed to load system health", Severity.Error);
            Log.Error(ex, "Failed to load system health");
        }
        finally
        {
            _isLoadingHealth = false;
        }
    }

    private static Color HealthColor(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => Color.Success,
        HealthStatus.Degraded => Color.Warning,
        _ => Color.Error
    };

    private static string HealthIcon(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => Icons.Material.Filled.CheckCircle,
        HealthStatus.Degraded => Icons.Material.Filled.Warning,
        _ => Icons.Material.Filled.Error
    };
}
