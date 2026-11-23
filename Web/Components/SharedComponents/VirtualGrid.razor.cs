using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Infrastructure;
using Web.Models;

namespace Web.Components.SharedComponents;

public sealed partial class VirtualGrid<TItem> : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<VirtualGrid<TItem>> Logger { get; set; } = default!;

    // The items shown; another source of other items is shown from the top, of the same items again in place.
    [Parameter, EditorRequired]
    public IVirtualRowSource<TItem> Source { get; set; } = default!;

    // An empty grid with the classes of the rows, whose columns are counted.
    [Parameter, EditorRequired]
    public RenderFragment Probe { get; set; } = default!;

    // A row: a grid of the items of the row.
    [Parameter, EditorRequired]
    public RenderFragment<VirtualRow<TItem>> RowContent { get; set; } = default!;

    // What tells two items apart, so that a row stays on screen while the rows around it arrive.
    [Parameter, EditorRequired]
    public Func<TItem, object> ItemKey { get; set; } = default!;

    // A guess of the height of a row, measured again by Virtualize once rendered.
    [Parameter]
    public float RowSize { get; set; } = 230;

    [Parameter]
    public string? RowClass { get; set; }

    // The item to open the grid on: the first one on screen when the page was left.
    [Parameter]
    public int InitialItem { get; set; }

    // The first item on screen, once the scroll stops.
    [Parameter]
    public EventCallback<int> OnPositionChanged { get; set; }

    [Parameter]
    public string LoadErrorMessage { get; set; } = "Failed to load";

    private readonly string _id = Guid.NewGuid().ToString("N");
    private readonly CancellationTokenSource _disposalCts = new();
    private VirtualRowComparer<TItem> _comparer = default!;
    private ElementReference _probe;
    private Virtualize<VirtualRow<TItem>>? _virtualize;
    private IJSObjectReference? _module;
    private DotNetObjectReference<VirtualGrid<TItem>>? _dotNetRef;
    private IVirtualRowSource<TItem>? _shownSource;
    private int _columns;
    private int _firstItem;

    protected override void OnInitialized() => _comparer = new VirtualRowComparer<TItem>(ItemKey);

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(_shownSource, Source))
        {
            return;
        }

        var otherItems = Source.ShowsOtherItemsThan(_shownSource);
        _shownSource = Source;
        if (_virtualize is null)
        {
            return;
        }

        await _virtualize.RefreshDataAsync();
        if (otherItems)
        {
            _firstItem = 0;
            await _virtualize.ScrollToItemAsync(0, _disposalCts.Token);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            var module = await JS.InvokeAsync<IJSObjectReference?>("import", _disposalCts.Token, AppVersion.Versioned("./js/virtualGrid.js"));
            if (module is null)
            {
                return;
            }

            _module = module;
            _firstItem = InitialItem;
            _columns = Math.Max(1, await module.InvokeAsync<int>("observe", _disposalCts.Token, _id, _probe, _dotNetRef));
            StateHasChanged();
        }
        catch (OperationCanceledException)
        {
            // Left before the script was loaded.
        }
        catch (JSException ex)
        {
            // Without the script, a row holds one item.
            Logger.LogWarning(ex, "Unable to measure the columns of a virtualized grid");
            _columns = 1;
            StateHasChanged();
        }
    }

    // The screen was resized or turned: the rows are cut again, and the first item on screen stays there.
    [JSInvokable]
    public async Task ColumnsChanged(int columns)
    {
        if (columns <= 0 || columns == _columns || _virtualize is null)
        {
            return;
        }

        var firstItem = _firstItem;
        _columns = columns;
        await _virtualize.RefreshDataAsync();
        await _virtualize.ScrollToItemAsync(firstItem / columns, _disposalCts.Token);
        StateHasChanged();
    }

    [JSInvokable]
    public Task PositionChanged(int firstItem)
    {
        _firstItem = firstItem;
        return OnPositionChanged.InvokeAsync(firstItem);
    }

    private async ValueTask<ItemsProviderResult<VirtualRow<TItem>>> LoadRowsAsync(ItemsProviderRequest request)
    {
        var columns = Math.Max(1, _columns);
        var result = await Source.GetRowsAsync(request.StartIndex, request.Count, columns, request.CancellationToken);
        if (result.IsSuccess)
        {
            return new ItemsProviderResult<VirtualRow<TItem>>(result.Value!.Rows, result.Value.RowCount);
        }

        Logger.LogError("Failed to load the items {Start}-{Count} of a virtualized grid: {Error}", request.StartIndex, request.Count, result.Error?.Description);
        Snackbar.Add(LoadErrorMessage, Severity.Error);
        return new ItemsProviderResult<VirtualRow<TItem>>([], 0);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("dispose", CancellationToken.None, _id);
                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // The circuit is gone: the script went with the page.
        }
        finally
        {
            _dotNetRef?.Dispose();
        }
    }
}
