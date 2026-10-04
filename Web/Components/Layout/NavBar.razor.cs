using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Web.Services;

namespace Web.Components.Layout;

public partial class NavBar : IDisposable
{
    private bool _isDisposed;
    private int _arbitrationCount;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    [Inject] private FeedImportNotifier FeedImportNotifier { get; set; } = default!;
    [Inject] private ILogger<NavBar> Logger { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += OnLocationChangedAsync;
        FeedImportNotifier.DecisionsChanged += OnFeedImportsChangedAsync;
        await RefreshArbitrationCountAsync();
    }

    private async void OnLocationChangedAsync(object? sender, LocationChangedEventArgs e)
    {
        await InvokeAsync(async () =>
        {
            await RefreshArbitrationCountAsync();
            StateHasChanged();
        });
    }

    private async void OnFeedImportsChangedAsync(object? sender, EventArgs e)
    {
        await InvokeAsync(async () =>
        {
            await RefreshArbitrationCountAsync();
            StateHasChanged();
        });
    }

    // The badge is informative: a failure is logged and hides it, it never breaks the navigation.
    // The NavBar lives beside the page in the same circuit: it queries through its own scope (own DbContext)
    // so its count never runs concurrently with the page's queries on the circuit's DbContext.
    private async Task RefreshArbitrationCountAsync()
    {
        if (AuthenticationState is null)
        {
            return;
        }

        try
        {
            await using var scope = ScopeFactory.CreateAsyncScope();
            if (scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>() is IHostEnvironmentAuthenticationStateProvider authStateProvider)
            {
                authStateProvider.SetAuthenticationState(AuthenticationState);
            }
            _arbitrationCount = await scope.ServiceProvider.GetRequiredService<IFeedImportService>().CountAwaitingArbitrationAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "NavBar: failed to count the feed imports awaiting arbitration");
            _arbitrationCount = 0;
        }
    }

    private string ArbitrationBadgeLabel => _arbitrationCount == 1
        ? "1 décision à arbitrer"
        : $"{_arbitrationCount} décisions à arbitrer";

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            // free managed resources
            NavigationManager.LocationChanged -= OnLocationChangedAsync;
            FeedImportNotifier.DecisionsChanged -= OnFeedImportsChangedAsync;
        }

        _isDisposed = true;
    }

    private bool IsActive(string path)
    {
        var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var queryOrFragmentIndex = relativePath.IndexOfAny(['?', '#']);
        if (queryOrFragmentIndex >= 0)
        {
            relativePath = relativePath[..queryOrFragmentIndex];
        }
        var trimmed = path.TrimStart('/');
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.IsNullOrEmpty(relativePath);
        }
        return relativePath.Equals(trimmed, StringComparison.OrdinalIgnoreCase) || relativePath.StartsWith(trimmed + "/", StringComparison.OrdinalIgnoreCase);
    }


}
