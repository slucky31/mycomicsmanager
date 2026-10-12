using Application.Books.Read;
using Application.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Infrastructure;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Books;

public sealed partial class BookReader : IAsyncDisposable
{
    // "?mode=isbn": the reader only looks for the ISBN, starts on the first page and never saves the progress.
    public const string IsbnSearchMode = "isbn";

    private static readonly TimeSpan s_saveProgressDelay = TimeSpan.FromSeconds(1);

    // The first OCR also downloads the engine and its language data.
    private static readonly TimeSpan s_isbnOcrTimeout = TimeSpan.FromMinutes(2);

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBookReaderService BookReaderService { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<BookReader> Logger { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    [SupplyParameterFromQuery(Name = "mode")]
    public string? Mode { get; set; }

    private Guid _bookGuid;
    private BookReaderInfoDto? _info;
    private bool _isLoading = true;
    private int _currentPage;
    private int _savedPage;
    private bool _showControls = true;
    private bool _showFinish;
    private int _rating = 3;
    private bool _isMarkingAsRead;
    private bool _isExtractingIsbn;
    private IReadOnlyList<string> _isbnCandidates = [];
    private IReadOnlyList<int> _quickPages = [];
    private ReaderPageWindow _pages = new(0);
    private bool _canUseFullscreen;
    private string? _scanProgress;

    private ElementReference _scroller;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _ocrModule;
    private DotNetObjectReference<BookReader>? _dotNetObjectRef;
    private CancellationTokenSource? _saveCts;
    private CancellationTokenSource? _scanCts;

    private bool IsIsbnSearch => string.Equals(Mode, IsbnSearchMode, StringComparison.OrdinalIgnoreCase);

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
                _savedPage = _info.LastReadPage;
                _currentPage = IsIsbnSearch ? 0 : _info.LastReadPage;
                _quickPages = IsIsbnSearch ? IsbnScanPages.GetPagesInReadingOrder(_info.PageCount) : [];
                _pages = new ReaderPageWindow(_info.PageCount);
                _pages.RequestAround(_currentPage);
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
            var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", AppVersion.Versioned("./js/bookReader.js"));
            var canUseFullscreen = await module.InvokeAsync<bool>("attach", _scroller, _dotNetObjectRef, _currentPage);
            _jsModule = module;
            _canUseFullscreen = canUseFullscreen;
            StateHasChanged();
        }
        catch (JSException ex)
        {
            // The pages can still be scrolled with the finger, but taps and shortcuts do nothing.
            Logger.LogWarning(ex, "Unable to attach the reader taps, keyboard and page tracking");
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

    // The reader scrolled to another page: the counter and the saved progress follow it.
    [JSInvokable]
    public Task OnVisiblePageChanged(int pageIndex) => InvokeAsync(() =>
    {
        if (_info is not null && pageIndex >= 0 && pageIndex < _info.PageCount && pageIndex != _currentPage)
        {
            SetCurrentPage(pageIndex);
        }
    });

    // The reader reached the bottom of the last page.
    [JSInvokable]
    public Task OnReachedEnd() => InvokeAsync(ShowFinish);

    [JSInvokable]
    public Task OnCenterTap() => InvokeAsync(() =>
    {
        ToggleControls();
        StateHasChanged();
    });

    private string PageUrl(int pageIndex) => $"/api/books/{_bookGuid}/pages/{pageIndex}";

    private Task PreviousPageAsync() => GoToPageAsync(_currentPage - 1);

    private Task NextPageAsync()
    {
        if (_info is not null && _currentPage >= _info.PageCount - 1)
        {
            ShowFinish();
            return Task.CompletedTask;
        }

        return GoToPageAsync(_currentPage + 1);
    }

    private void ShowFinish()
    {
        // Looking for the ISBN on the back cover is not finishing the book.
        _showFinish = !IsIsbnSearch;
        StateHasChanged();
    }

    private Task OnSliderChangedAsync(int pageNumber) => GoToPageAsync(pageNumber - 1);

    private async Task GoToPageAsync(int pageIndex)
    {
        if (_info is null || _jsModule is null || pageIndex < 0 || pageIndex >= _info.PageCount || pageIndex == _currentPage)
        {
            return;
        }

        try
        {
            await _jsModule.InvokeVoidAsync("scrollToPage", pageIndex);
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to scroll to page {PageIndex} of book {BookId}", pageIndex, BookId);
            return;
        }

        SetCurrentPage(pageIndex);
    }

    private void SetCurrentPage(int pageIndex)
    {
        _currentPage = pageIndex;
        _pages.RequestAround(pageIndex);
        StateHasChanged();
        ScheduleProgressSave();
    }

    private void ToggleControls() => _showControls = !_showControls;

    private async Task ToggleFullscreenAsync()
    {
        if (_jsModule is null)
        {
            return;
        }

        try
        {
            await _jsModule.InvokeVoidAsync("toggleFullscreen");
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to toggle the full screen mode of the reader");
        }
    }

    private void CloseFinish() => _showFinish = false;

    private void CloseIsbnCandidates() => _isbnCandidates = [];

    // The OCR runs in the browser on the page being read (already in its cache): only its text comes back.
    private async Task ExtractIsbnAsync()
    {
        if (_isExtractingIsbn)
        {
            return;
        }

        var pageIndex = _currentPage;
        _isExtractingIsbn = true;

        try
        {
            var module = await GetOcrModuleAsync();
            var text = await module.InvokeAsync<string>("recognizeUrl", s_isbnOcrTimeout, PageUrl(pageIndex));
            if (pageIndex != _currentPage)
            {
                // The reader turned the page meanwhile: this text belongs to another page.
                return;
            }

            ShowIsbnCandidates(TextIsbnExtractor.ExtractAll(text));
        }
        catch (Exception ex) when (ex is JSException or OperationCanceledException)
        {
            Snackbar.Add("Unable to read the text of this page", Severity.Error);
            Logger.LogError(ex, "Unable to read the text of page {PageIndex} of book {BookId}", pageIndex, BookId);
        }
        finally
        {
            _isExtractingIsbn = false;
        }
    }

    // Reads the first and last pages one by one, the most likely first, until one shows an ISBN.
    private async Task ScanIsbnPagesAsync()
    {
        if (_info is null || _isExtractingIsbn)
        {
            return;
        }

        var pages = IsbnScanPages.GetScanOrder(_info.PageCount);
        if (_scanCts is not null)
        {
            await _scanCts.CancelAsync();
            _scanCts.Dispose();
        }

        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;
        _isExtractingIsbn = true;

        try
        {
            var module = await GetOcrModuleAsync();
            for (var i = 0; i < pages.Count; i++)
            {
                _scanProgress = $"{i + 1} / {pages.Count}";
                StateHasChanged();

                var isbns = await ReadIsbnsAsync(module, pages[i], token);
                if (isbns.Count > 0)
                {
                    // Shows the page where the ISBN is printed, so the reader can check it.
                    await GoToPageAsync(pages[i]);
                    _isbnCandidates = isbns;
                    return;
                }
            }

            Snackbar.Add("No ISBN found in the first and last pages", Severity.Warning);
        }
        catch (Exception ex) when (token.IsCancellationRequested || ex is JSDisconnectedException)
        {
            // The reader was closed during the scan.
        }
        catch (Exception ex) when (ex is JSException or OperationCanceledException)
        {
            Snackbar.Add("Unable to read the text of the pages", Severity.Error);
            Logger.LogError(ex, "Unable to scan the pages of book {BookId} for its ISBN", BookId);
        }
        finally
        {
            _isExtractingIsbn = false;
            _scanProgress = null;
        }
    }

    private async Task<IReadOnlyList<string>> ReadIsbnsAsync(IJSObjectReference module, int pageIndex, CancellationToken token)
    {
        using var pageCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        pageCts.CancelAfter(s_isbnOcrTimeout);
        var text = await module.InvokeAsync<string>("recognizeUrl", pageCts.Token, PageUrl(pageIndex));
        return TextIsbnExtractor.ExtractAll(text);
    }

    private async Task<IJSObjectReference> GetOcrModuleAsync()
    {
        var module = _ocrModule ?? await JSRuntime.InvokeAsync<IJSObjectReference>("import", AppVersion.Versioned("./js/isbnOcr.js"));
        _ocrModule = module;
        return module;
    }

    private void ShowIsbnCandidates(IReadOnlyList<string> isbns)
    {
        if (isbns.Count == 0)
        {
            Snackbar.Add("No ISBN found on this page", Severity.Warning);
            return;
        }

        _isbnCandidates = isbns;
    }

    private async Task FetchBookInfoAsync(string isbn)
    {
        await SaveProgressAsync(_currentPage);
        NavigationManager.NavigateTo($"/books/{BookId}/find-details?isbn={Uri.EscapeDataString(isbn)}");
    }

    // Saving on every page turn would hammer the database while flipping through a book:
    // the progress is only written once the reader stays on a page for a moment.
    private void ScheduleProgressSave()
    {
        if (IsIsbnSearch)
        {
            return;
        }

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
        if (_info is null || IsIsbnSearch || pageIndex == _savedPage)
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

        if (_scanCts is not null)
        {
            await _scanCts.CancelAsync();
            _scanCts.Dispose();
            _scanCts = null;
        }

        // Leaving the page another way (browser back, menu) must not lose the progress.
        await SaveProgressAsync(_currentPage);

        await ReleaseModuleAsync(_jsModule, "detach");
        await ReleaseModuleAsync(_ocrModule, "terminate");

        _dotNetObjectRef?.Dispose();
    }

    private static async Task ReleaseModuleAsync(IJSObjectReference? module, string cleanupFunction)
    {
        if (module is null)
        {
            return;
        }

        try
        {
            await module.InvokeVoidAsync(cleanupFunction, CancellationToken.None);
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone: the browser already released the module's resources.
        }
        catch (JSException)
        {
            // Ignore
        }
    }
}
