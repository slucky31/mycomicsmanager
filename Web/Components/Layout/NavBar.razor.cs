using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Web.Models;
using Web.Services;

namespace Web.Components.Layout;

public partial class NavBar : IDisposable
{
    private bool _isDisposed;
    private int _arbitrationCount;
    private IReadOnlyList<NavLibraryItemViewModel> _libraries = [];
    private Guid? _currentLibraryId;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    [Inject] private FeedImportNotifier FeedImportNotifier { get; set; } = default!;
    [Inject] private ILogger<NavBar> Logger { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += OnLocationChangedAsync;
        FeedImportNotifier.DecisionsChanged += OnFeedImportsChangedAsync;
        UpdateCurrentLibrary();
        await RefreshAsync();
    }

    private async void OnLocationChangedAsync(object? sender, LocationChangedEventArgs e)
    {
        await InvokeAsync(async () =>
        {
            UpdateCurrentLibrary();
            await RefreshAsync();
            StateHasChanged();
        });
    }

    private async void OnFeedImportsChangedAsync(object? sender, EventArgs e)
    {
        await InvokeAsync(async () =>
        {
            await RefreshAsync();
            StateHasChanged();
        });
    }

    private string RelativePath => NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

    private void UpdateCurrentLibrary() => _currentLibraryId = NavigationRules.CurrentLibraryId(RelativePath);

    // The badge and the library list are informative: a failure is logged and hides it, it never breaks the navigation.
    // The NavBar lives beside the page in the same circuit: it queries through its own scope (own DbContext)
    // so its queries never run concurrently with the page's queries on the circuit's DbContext.
    private async Task RefreshAsync()
    {
        if (AuthenticationState is null)
        {
            return;
        }

        await using var scope = ScopeFactory.CreateAsyncScope();
        if (scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>() is IHostEnvironmentAuthenticationStateProvider authStateProvider)
        {
            authStateProvider.SetAuthenticationState(AuthenticationState);
        }

        await RefreshArbitrationCountAsync(scope.ServiceProvider);
        await RefreshLibrariesAsync(scope.ServiceProvider);
    }

    private async Task RefreshArbitrationCountAsync(IServiceProvider services)
    {
        try
        {
            _arbitrationCount = await services.GetRequiredService<IFeedImportService>().CountAwaitingArbitrationAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "NavBar: failed to count the feed imports awaiting arbitration");
            _arbitrationCount = 0;
        }
    }

    private async Task RefreshLibrariesAsync(IServiceProvider services)
    {
        try
        {
            var result = await services.GetRequiredService<ILibrariesService>().GetSummaries();
            if (result.IsSuccess && result.Value is not null)
            {
                _libraries = [.. result.Value.Select(NavLibraryItemViewModel.From)];
                return;
            }

            Logger.LogError("NavBar: failed to load the libraries: {Error}", result.Error?.Description);
            _libraries = [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "NavBar: failed to load the libraries");
            _libraries = [];
        }
    }

    private string ArbitrationBadgeLabel => _arbitrationCount == 1
        ? "1 decision awaiting arbitration"
        : $"{_arbitrationCount} decisions awaiting arbitration";

    private string FeedsLabel => _arbitrationCount > 0 ? $"Feeds, {ArbitrationBadgeLabel}" : "Feeds";

    // On a library page the side bar highlights that library; the Libraries entry stays marked as its parent.
    private bool IsLibrariesEntryActive => _currentLibraryId is null && (IsActive("/libraries") || IsActive("/"));

    private string LibrariesEntryClass
    {
        get
        {
            if (IsLibrariesEntryActive)
            {
                return "active";
            }

            return _currentLibraryId is null ? string.Empty : "active-parent";
        }
    }

    private static string ActiveClass(bool isActive) => isActive ? "active" : string.Empty;

    private static string? AriaCurrent(bool isActive) => isActive ? "page" : null;

    private bool IsActive(string path) => NavigationRules.IsActive(RelativePath, path);

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
}
