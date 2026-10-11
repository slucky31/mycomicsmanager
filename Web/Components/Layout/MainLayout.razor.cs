using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Web.Models;

namespace Web.Components.Layout;

public partial class MainLayout : IDisposable
{
    private bool _isDisposed;
    private bool _showNavigation = true;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        UpdateNavigationVisibility();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var showNavigation = _showNavigation;
        UpdateNavigationVisibility();
        if (showNavigation != _showNavigation)
        {
            InvokeAsync(StateHasChanged);
        }
    }

    private void UpdateNavigationVisibility() =>
        _showNavigation = !NavigationRules.HidesNavigation(NavigationManager.ToBaseRelativePath(NavigationManager.Uri));

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
