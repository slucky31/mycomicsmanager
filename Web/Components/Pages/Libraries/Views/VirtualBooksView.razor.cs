using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Infrastructure;
using Web.Models;

namespace Web.Components.Pages.Libraries.Views;

public sealed partial class VirtualBooksView : IAsyncDisposable
{
    private static readonly IReadOnlyList<BookListItemViewModel> NoBooks = [];

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<VirtualBooksView> Logger { get; set; } = default!;

    // The books of the current sort and search; another instance is shown from the top, the same books again in place.
    [Parameter, EditorRequired]
    public LibraryBookRows Rows { get; set; } = default!;

    [Parameter]
    public LibraryView View { get; set; } = LibraryView.Covers;

    // The book to open the library on: the first one on screen when it was left.
    [Parameter]
    public int InitialBook { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<BookListItemViewModel> OnActions { get; set; }

    [Parameter]
    public BookSelection Selection { get; set; } = BookSelection.None;

    [Parameter]
    public EventCallback<BookListItemViewModel> OnToggle { get; set; }

    [Parameter]
    public string? From { get; set; }

    // The first book on screen, once the scroll stops.
    [Parameter]
    public EventCallback<int> OnPositionChanged { get; set; }

    private readonly string _id = Guid.NewGuid().ToString("N");
    private readonly CancellationTokenSource _disposalCts = new();
    private ElementReference _probe;
    private Virtualize<BookRow>? _virtualize;
    private IJSObjectReference? _module;
    private DotNetObjectReference<VirtualBooksView>? _dotNetRef;
    private LibraryBookRows? _shownRows;
    private int _columns;
    private int _firstBook;

    // A guess of the height of a row, measured again by Virtualize once rendered.
    private float RowSize => View == LibraryView.List ? 112 : 230;

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(_shownRows, Rows))
        {
            return;
        }

        var otherBooks = Rows.ShowsOtherBooksThan(_shownRows);
        _shownRows = Rows;
        if (_virtualize is null)
        {
            return;
        }

        await _virtualize.RefreshDataAsync();
        if (otherBooks)
        {
            _firstBook = 0;
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
            var module = await JS.InvokeAsync<IJSObjectReference?>("import", _disposalCts.Token, AppVersion.Versioned("./js/virtualBooks.js"));
            if (module is null)
            {
                return;
            }

            _module = module;
            _firstBook = InitialBook;
            _columns = Math.Max(1, await module.InvokeAsync<int>("observe", _disposalCts.Token, _id, _probe, _dotNetRef));
            StateHasChanged();
        }
        catch (OperationCanceledException)
        {
            // Left before the script was loaded.
        }
        catch (JSException ex)
        {
            // Without the script, a row holds one book.
            Logger.LogWarning(ex, "Unable to measure the columns of the virtualized library");
            _columns = 1;
            StateHasChanged();
        }
    }

    // The screen was resized or turned: the rows are cut again, and the first book on screen stays there.
    [JSInvokable]
    public async Task ColumnsChanged(int columns)
    {
        if (columns <= 0 || columns == _columns || _virtualize is null)
        {
            return;
        }

        var firstBook = _firstBook;
        _columns = columns;
        await _virtualize.RefreshDataAsync();
        await _virtualize.ScrollToItemAsync(firstBook / columns, _disposalCts.Token);
        StateHasChanged();
    }

    [JSInvokable]
    public Task PositionChanged(int firstBook)
    {
        _firstBook = firstBook;
        return OnPositionChanged.InvokeAsync(firstBook);
    }

    private async ValueTask<ItemsProviderResult<BookRow>> LoadRowsAsync(ItemsProviderRequest request)
    {
        var rows = Rows;
        var columns = Math.Max(1, _columns);
        var result = await rows.GetRowsAsync(request.StartIndex, request.Count, columns, request.CancellationToken);
        if (result.IsSuccess)
        {
            return new ItemsProviderResult<BookRow>(result.Value!.Rows, result.Value.RowCount);
        }

        Logger.LogError("Failed to load the books {Start}-{Count} of the virtualized library: {Error}", request.StartIndex, request.Count, result.Error?.Description);
        Snackbar.Add("Failed to load books", Severity.Error);
        return new ItemsProviderResult<BookRow>([], 0);
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
