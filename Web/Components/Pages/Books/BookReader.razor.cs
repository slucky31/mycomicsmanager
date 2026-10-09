using Application.Books.Read;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Services;

namespace Web.Components.Pages.Books;

public sealed partial class BookReader : IAsyncDisposable
{
    private static readonly TimeSpan s_saveProgressDelay = TimeSpan.FromSeconds(1);

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBookReaderService BookReaderService { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<BookReader> Logger { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    private Guid _bookGuid;
    private BookReaderInfoDto? _info;
    private bool _isLoading = true;
    private int _currentPage;
    private int _savedPage;
    private bool _showControls = true;
    private bool _showFinish;
    private int _rating = 3;
    private bool _isMarkingAsRead;

    private ElementReference _surface;
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<BookReader>? _dotNetObjectRef;
    private CancellationTokenSource? _saveCts;

    protected override async Task OnInitializedAsync()
    {
        if (!Guid.TryParse(BookId, out _bookGuid))
        {
            _isLoading = false;
            return;
        }

        try
        {
            var result = await BookReaderService.GetReaderInfoAsync(_bookGuid, CancellationToken.None);
            if (result.IsSuccess && result.Value is not null)
            {
                _info = result.Value;
                _currentPage = _info.LastReadPage;
                _savedPage = _currentPage;
            }
            else if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to open this book", Severity.Error);
                Logger.LogError("Unable to open book {BookId} in the reader: {Error}", BookId, result.Error?.Code);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add("Unexpected error while opening the book", Severity.Error);
            Logger.LogError(ex, "Unexpected error while opening book {BookId} in the reader", BookId);
        }
        finally
        {
            _isLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_info is null || _jsModule is not null)
        {
            return;
        }

        try
        {
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/bookReader.js");
            await module.InvokeVoidAsync("attach", _surface, _dotNetObjectRef);
            _jsModule = module;
        }
        catch (JSException ex)
        {
            // Keyboard and swipe are a convenience: the on-screen controls still work.
            Logger.LogWarning(ex, "Unable to attach the reader keyboard and swipe navigation");
        }
    }

    [JSInvokable]
    public Task OnNavigationKey(string key) => key switch
    {
        "ArrowLeft" => InvokeAsync(PreviousPageAsync),
        "ArrowRight" => InvokeAsync(NextPageAsync),
        "Escape" => InvokeAsync(CloseAsync),
        _ => Task.CompletedTask,
    };

    private string PageUrl(int pageIndex) => $"/api/books/{_bookGuid}/pages/{pageIndex}";

    private Task PreviousPageAsync() => GoToPageAsync(_currentPage - 1);

    private Task NextPageAsync()
    {
        if (_info is not null && _currentPage >= _info.PageCount - 1)
        {
            _showFinish = true;
            StateHasChanged();
            return Task.CompletedTask;
        }

        return GoToPageAsync(_currentPage + 1);
    }

    private Task OnSliderChangedAsync(int pageNumber) => GoToPageAsync(pageNumber - 1);

    private Task GoToPageAsync(int pageIndex)
    {
        if (_info is null || pageIndex < 0 || pageIndex >= _info.PageCount || pageIndex == _currentPage)
        {
            return Task.CompletedTask;
        }

        _currentPage = pageIndex;
        StateHasChanged();
        ScheduleProgressSave();
        return Task.CompletedTask;
    }

    private void ToggleControls() => _showControls = !_showControls;

    private void CloseFinish() => _showFinish = false;

    // Saving on every page turn would hammer the database while flipping through a book:
    // the progress is only written once the reader stays on a page for a moment.
    private void ScheduleProgressSave()
    {
        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(s_saveProgressDelay, token);
                await InvokeAsync(() => SaveProgressAsync(_currentPage));
            }
            catch (OperationCanceledException)
            {
                // A newer page turn superseded this save.
            }
        }, CancellationToken.None);
    }

    private async Task SaveProgressAsync(int pageIndex)
    {
        if (_info is null || pageIndex == _savedPage)
        {
            return;
        }

        try
        {
            var result = await BookReaderService.SaveProgressAsync(_bookGuid, pageIndex, CancellationToken.None);
            if (result.IsSuccess)
            {
                _savedPage = pageIndex;
            }
            else
            {
                Logger.LogWarning("Unable to save the reading progress of book {BookId}: {Error}", BookId, result.Error?.Code);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Logger.LogWarning(ex, "Unable to save the reading progress of book {BookId}", BookId);
        }
    }

    private async Task MarkAsReadAsync()
    {
        _isMarkingAsRead = true;

        try
        {
            var result = await BooksService.AddReadingDate(BookId, _rating, CancellationToken.None);
            if (result.IsFailure)
            {
                Snackbar.Add("Unexpected error while adding reading date", Severity.Error);
                Logger.LogError("Unexpected error while adding reading date: {Error}", result.Error?.Code);
                return;
            }

            // The book is finished: the next reading starts again from the first page.
            _currentPage = 0;
            await SaveProgressAsync(0);
            Snackbar.Add("Reading date added", Severity.Success);
            NavigationManager.NavigateTo($"/books/{BookId}");
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add("Unexpected error while adding reading date", Severity.Error);
            Logger.LogError(ex, "Unexpected error while adding reading date");
        }
        finally
        {
            _isMarkingAsRead = false;
        }
    }

    private async Task CloseAsync()
    {
        await SaveProgressAsync(_currentPage);
        NavigationManager.NavigateTo($"/books/{BookId}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_saveCts is not null)
        {
            await _saveCts.CancelAsync();
            _saveCts.Dispose();
            _saveCts = null;
        }

        // Leaving the page another way (browser back, menu) must not lose the progress.
        await SaveProgressAsync(_currentPage);

        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("detach", CancellationToken.None);
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone: the browser already dropped the listeners.
            }
            catch (JSException)
            {
                // Ignore
            }
        }

        _dotNetObjectRef?.Dispose();
    }
}
