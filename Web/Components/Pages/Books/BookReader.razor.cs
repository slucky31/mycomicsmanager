using Application.Books.Read;
using Application.Helpers;
using Domain.Books;
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

    private static readonly DisplayOption<ReaderLayout>[] s_layoutOptions =
    [
        new(ReaderLayout.FitWidth, "Fit width", Icons.Material.Filled.WidthFull),
        new(ReaderLayout.SinglePage, "Single page", Icons.Material.Filled.CropPortrait),
        new(ReaderLayout.TwoPages, "Two pages", Icons.Material.Filled.MenuBook),
    ];

    private static readonly DisplayOption<ReaderScrolling>[] s_scrollingOptions =
    [
        new(ReaderScrolling.Continuous, "Continuous", Icons.Material.Filled.SwapVert),
        new(ReaderScrolling.PageByPage, "Page by page", Icons.Material.Filled.AutoStories),
    ];

    private static readonly DisplayOption<ReadingDirection>[] s_directionOptions =
    [
        new(ReadingDirection.LeftToRight, "Left to right", Icons.Material.Filled.East),
        new(ReadingDirection.RightToLeft, "Right to left", Icons.Material.Filled.West),
    ];

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
    private ReaderSettings _settings = ReaderSettings.Default;
    private ReaderSpreads? _spreads;
    private int _currentRow;
    private bool _canUseFullscreen;
    private bool _showDisplayPanel;
    private bool _showGestureHelp;
    // The rows were rendered again for new settings: the script must be told before it measures them.
    private bool _pendingConfigure;
    // Page by page, a new page was rendered: the script shows its top.
    private bool _pendingScroll;
    private string? _scanProgress;

    private ElementReference _scroller;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _ocrModule;
    private DotNetObjectReference<BookReader>? _dotNetObjectRef;
    private CancellationTokenSource? _saveCts;
    private CancellationTokenSource? _scanCts;

    private bool IsIsbnSearch => string.Equals(Mode, IsbnSearchMode, StringComparison.OrdinalIgnoreCase);

    private bool IsPaged => _settings.Scrolling == ReaderScrolling.PageByPage;

    private bool IsRightToLeft => _settings.Direction == ReadingDirection.RightToLeft;

    private bool IsFirstRow => _currentRow == 0;

    private bool IsLastRow => _spreads is null || _currentRow >= _spreads.Count - 1;

    private ReaderScriptOptions ScriptOptions => ReaderScriptOptions.From(_settings);

    private string ScrollerClass
    {
        get
        {
            var layout = _settings.Layout switch
            {
                ReaderLayout.SinglePage => "reader-layout-single",
                ReaderLayout.TwoPages => "reader-layout-two",
                _ => "reader-layout-fit",
            };
            // Continuously scrolled pages that fill the screen snap into place.
            var snap = !IsPaged && _settings.Layout != ReaderLayout.FitWidth ? " reader-snap" : string.Empty;
            return $"reader-scroll {layout}{snap}";
        }
    }

    // Page by page, only the current row is in the document.
    private IEnumerable<int> DisplayedRows
    {
        get
        {
            if (_spreads is null)
            {
                return [];
            }

            return IsPaged ? [_currentRow] : Enumerable.Range(0, _spreads.Count);
        }
    }

    private IEnumerable<int> PreloadedPages => IsPaged && _spreads is not null && !IsLastRow
        ? _spreads.PagesOf(_currentRow + 1)
        : [];

    private static string OptionClass(bool selected) => selected ? "reader-option reader-option-selected" : "reader-option";

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
                _quickPages = IsIsbnSearch ? IsbnScanPages.GetPagesInReadingOrder(_info.PageCount) : [];
                _pages = new ReaderPageWindow(_info.PageCount);
                _settings = _info.Settings;
                _spreads = ReaderSpreads.Build(_info.PageCount, _settings);
                ShowRow(_spreads.RowOf(IsIsbnSearch ? 0 : _info.LastReadPage));
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
        if (_info is null)
        {
            return;
        }

        if (_jsModule is null)
        {
            await AttachScriptAsync();
        }
        else if (_pendingConfigure)
        {
            await ConfigureScriptAsync();
        }
        else if (_pendingScroll)
        {
            await ScrollToCurrentRowAsync();
        }
    }

    private async Task AttachScriptAsync()
    {
        try
        {
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", AppVersion.Versioned("./js/bookReader.js"));
            var clientState = await module.InvokeAsync<ReaderClientState?>("attach", _scroller, _dotNetObjectRef, _currentRow, ScriptOptions);
            _jsModule = module;
            _pendingConfigure = false;
            _pendingScroll = false;
            _canUseFullscreen = clientState?.CanUseFullscreen ?? false;
            _showGestureHelp = clientState is { GestureHelpSeen: false };
            StateHasChanged();
        }
        catch (JSException ex)
        {
            // The pages can still be scrolled with the finger, but taps and shortcuts do nothing.
            Logger.LogWarning(ex, "Unable to attach the reader taps, keyboard and page tracking");
        }
    }

    private async Task ConfigureScriptAsync()
    {
        try
        {
            await _jsModule!.InvokeVoidAsync("configure", CancellationToken.None, ScriptOptions, _currentRow);
            _pendingConfigure = false;
            _pendingScroll = false;
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to apply the display settings of book {BookId} to the reader", BookId);
        }
    }

    private async Task ScrollToCurrentRowAsync()
    {
        try
        {
            await _jsModule!.InvokeVoidAsync("scrollToRow", CancellationToken.None, _currentRow);
            _pendingScroll = false;
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to show page {PageIndex} of book {BookId}", _currentPage, BookId);
        }
    }

    [JSInvokable]
    public Task OnNavigationKey(string key) => key switch
    {
        "ArrowLeft" => InvokeAsync(LeftArrowAsync),
        "ArrowRight" => InvokeAsync(RightArrowAsync),
        "Escape" => InvokeAsync(CloseAsync),
        _ => Task.CompletedTask,
    };

    // The reader scrolled to another row: the counter and the saved progress follow it.
    [JSInvokable]
    public Task OnVisibleRowChanged(int row) => InvokeAsync(() =>
    {
        if (_spreads is not null && row >= 0 && row < _spreads.Count && row != _currentRow)
        {
            SetCurrentRow(row);
        }
    });

    // Page by page: a tap, a swipe or the space bar went past the page shown.
    [JSInvokable]
    public Task OnTurnPage(int direction) => InvokeAsync(direction > 0 ? NextPageAsync : PreviousPageAsync);

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

    // Mangas are read from right to left: the arrow pointing left goes to the next page.
    private Task LeftArrowAsync() => IsRightToLeft ? NextPageAsync() : PreviousPageAsync();

    private Task RightArrowAsync() => IsRightToLeft ? PreviousPageAsync() : NextPageAsync();

    private Task PreviousPageAsync() => GoToRowAsync(_currentRow - 1);

    private Task NextPageAsync()
    {
        if (IsLastRow)
        {
            ShowFinish();
            return Task.CompletedTask;
        }

        return GoToRowAsync(_currentRow + 1);
    }

    private void ShowFinish()
    {
        // Looking for the ISBN on the back cover is not finishing the book.
        _showFinish = !IsIsbnSearch;
        StateHasChanged();
    }

    private Task OnSliderChangedAsync(int pageNumber) => GoToPageAsync(pageNumber - 1);

    private Task GoToPageAsync(int pageIndex) =>
        _info is null || pageIndex < 0 || pageIndex >= _info.PageCount
            ? Task.CompletedTask
            : GoToRowAsync(_spreads!.RowOf(pageIndex));

    private async Task GoToRowAsync(int row)
    {
        if (_spreads is null || row < 0 || row >= _spreads.Count || row == _currentRow)
        {
            return;
        }

        if (IsPaged)
        {
            // The new page is rendered first, then shown from its top.
            _pendingScroll = true;
            SetCurrentRow(row);
            return;
        }

        if (_jsModule is null)
        {
            return;
        }

        try
        {
            await _jsModule.InvokeVoidAsync("scrollToRow", CancellationToken.None, row);
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to scroll to page {PageIndex} of book {BookId}", _spreads.FirstPageOf(row), BookId);
            return;
        }

        SetCurrentRow(row);
    }

    private void SetCurrentRow(int row)
    {
        ShowRow(row);
        StateHasChanged();
        ScheduleProgressSave();
    }

    // The progress points at the first page of the row; the pages around it are loaded.
    private void ShowRow(int row)
    {
        _currentRow = row;
        _currentPage = _spreads!.FirstPageOf(row);
        _pages.RequestAround(_currentPage);
        _pages.RequestAround(_spreads.LastPageOf(row));
    }

    private void OpenDisplayPanel() => _showDisplayPanel = true;

    private void CloseDisplayPanel() => _showDisplayPanel = false;

    private Task ChangeLayoutAsync(ReaderLayout layout) => ApplySettingsAsync(_settings with { Layout = layout });

    private Task ChangeScrollingAsync(ReaderScrolling scrolling) => ApplySettingsAsync(_settings with { Scrolling = scrolling });

    private Task ChangeDirectionAsync(ReadingDirection direction) => ApplySettingsAsync(_settings with { Direction = direction });

    // The new layout shows the same page, and is kept for the next opening of this book.
    private async Task ApplySettingsAsync(ReaderSettings settings)
    {
        if (_info is null || settings == _settings)
        {
            return;
        }

        _settings = settings;
        _spreads = ReaderSpreads.Build(_info.PageCount, settings);
        ShowRow(_spreads.RowOf(_currentPage));
        _pendingConfigure = true;
        StateHasChanged();

        try
        {
            var result = await BookReaderService.SaveSettingsAsync(_bookGuid, settings, CancellationToken.None);
            if (result.IsFailure)
            {
                Snackbar.Add("Unable to save the display settings", Severity.Warning);
                Logger.LogWarning("Unable to save the display settings of book {BookId}: {Error}", BookId, result.Error?.Code);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add("Unable to save the display settings", Severity.Warning);
            Logger.LogWarning(ex, "Unable to save the display settings of book {BookId}", BookId);
        }
    }

    private async Task DismissGestureHelpAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("markGestureHelpSeen", CancellationToken.None);
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Unable to remember that the reader gesture help was seen");
            }
        }

        _showGestureHelp = false;
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
            await _jsModule.InvokeVoidAsync("toggleFullscreen", CancellationToken.None);
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

internal sealed record DisplayOption<T>(T Value, string Label, string Icon);
