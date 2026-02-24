using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Web.Components.Layout;

public partial class NavBar : IDisposable
{
    private bool _isDisposed;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChangedAsync;
    }

    private async void OnLocationChangedAsync(object? sender, LocationChangedEventArgs e)
    {
        await InvokeAsync(StateHasChanged);
    }

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
