using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Web.Models;

namespace Web.Components.Layout;

public partial class MainLayout : IDisposable
{
    private bool _isDisposed;
    private bool _showBottomBar = true;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        UpdateNavigationVisibility();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var showBottomBar = _showBottomBar;
        UpdateNavigationVisibility();
        if (showBottomBar != _showBottomBar)
        {
            InvokeAsync(StateHasChanged);
        }
    }

    private void UpdateNavigationVisibility() =>
        _showBottomBar = !NavigationRules.HidesBottomBar(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));

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
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        _isDisposed = true;
    }
}
