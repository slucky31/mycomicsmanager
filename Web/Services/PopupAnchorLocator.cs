using Microsoft.JSInterop;
using Web.Infrastructure;
using Web.Models;

namespace Web.Services;

// Where a menu opens from on a tablet or a large screen (popupAnchor.js). Without the script the menu is centered.
// One per circuit: the script is loaded on the first menu opened.
public sealed class PopupAnchorLocator(IJSRuntime js, ILogger<PopupAnchorLocator> logger) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task<PopupAnchor?> AnchorOfAsync(string elementId, CancellationToken cancellationToken = default)
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, AppVersion.Versioned("./js/popupAnchor.js"));
            return await _module.InvokeAsync<PopupAnchor?>("anchorOf", cancellationToken, elementId);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Unable to find the position of {ElementId}", elementId);
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // JS interop can fail when the Blazor circuit is disconnected; intentionally ignored.
        }
    }
}
