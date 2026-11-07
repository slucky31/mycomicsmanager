using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Web.Components.SharedComponents;

// Placed after the last item of a list: asks for the next page when it comes near the screen.
// Shown only while there is a next page, so that it is not observed for nothing.
public sealed partial class InfiniteScrollSentinel : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<InfiniteScrollSentinel> Logger { get; set; } = default!;

    // Loads the next page; the list ignores a call while it is already loading.
    [Parameter, EditorRequired] public EventCallback OnLoadMore { get; set; }

    // The number of items shown: the sentinel is observed again after each page, so that a sentinel
    // still on screen (a short page) asks for the next one.
    [Parameter] public int ItemCount { get; set; }

    // The element that scrolls; the page itself when none is given.
    [Parameter] public string? ScrollContainer { get; set; }

    private readonly string _sentinelId = $"scroll-sentinel-{Guid.NewGuid():N}";
    private readonly CancellationTokenSource _disposalCts = new();
    private DotNetObjectReference<InfiniteScrollSentinel>? _dotNetRef;
    private int? _observedCount;

    protected override void OnInitialized() => _dotNetRef = DotNetObjectReference.Create(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_observedCount == ItemCount)
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("infiniteScroll.observe", _disposalCts.Token, _dotNetRef, _sentinelId, ScrollContainer, nameof(LoadMoreAsync));
            _observedCount = ItemCount;
        }
        catch (Exception ex) when (ex is OperationCanceledException or JSDisconnectedException)
        {
            // Disposed while observing; nothing to update.
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to observe the end of the list");
        }
    }

    [JSInvokable]
    public Task LoadMoreAsync() => InvokeAsync(() => OnLoadMore.InvokeAsync());

    public async ValueTask DisposeAsync()
    {
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
        try
        {
            await JS.InvokeVoidAsync("infiniteScroll.dispose", CancellationToken.None, _sentinelId);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // JS interop can fail when the circuit is disconnected or during static prerendering; nothing to stop then.
        }
        finally
        {
            _dotNetRef?.Dispose();
        }
    }
}
