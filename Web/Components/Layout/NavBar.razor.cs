using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Web.Components.Layout;

public partial class NavBar : IDisposable
{
    private bool _isDisposed;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);        
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // free managed resources
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        _isDisposed = true;
    }

    private bool IsActive(string path)
    {
        return NavigationManager.Uri.Contains(path, StringComparison.OrdinalIgnoreCase);
    }
}
