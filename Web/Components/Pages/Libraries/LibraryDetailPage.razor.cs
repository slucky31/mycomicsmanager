using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Components.Pages.Libraries.Views;
using Web.Extensions;
using Web.Infrastructure;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class LibraryDetailPage : IAsyncDisposable
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IBookMoveWorkflow BookMoveWorkflow { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private LibraryStateService LibraryStateService { get; set; } = default!;
    [Inject] private ILogger<LibraryDetailPage> Logger { get; set; } = default!;
    [Inject] private IIsbnScanService IsbnScanService { get; set; } = default!;

    [Parameter] public string? LibraryId { get; set; }

    internal const int PageSize = 24;
    private const string SortAndViewButtonId = "library-sort-and-view";
    private const string MoreActionsButtonId = "library-more-actions";
    private const string AddBookButtonId = "library-add-book";
    private const string AddBookIconId = "library-add-book-icon";

    private enum LibrarySheet
    {
        None,
        SortAndView,
        MoreActions,
        BookActions,
        AddBook
    }

    private LibraryUiDto? _library;
    private bool _isStartingIsbnScan;
    private Guid _libraryGuid;

    // Pages loaded so far (infinite scroll), the same for both views.
    private readonly List<BookListItemViewModel> _displayedBooks = [];
    private int _currentPage = 1;
    private bool _hasNextPage;
    private bool _isLoadingMore;
    // Bumped by every reload of the first page: a next page asked for before it is dropped.
    private int _loadVersion;

    // Number of books of the library (known once a page was loaded without search) and of the shown results.
    private int? _libraryBookCount;
    private int _resultCount;
    // The search of the books on screen (the typed one is applied after a pause).
    private string _shownSearch = string.Empty;

    private bool _isLoading = true;
    private bool _isMoving;
    private bool _isDeleting;
    private bool _observerInitialized;
    private bool _isSearchOpen;
    private bool _focusSearch;

    private LibrarySheet _openSheet;
    private PopupAnchor? _sheetAnchor;
    private bool _sheetAlignEnd;
    private BookListItemViewModel? _actionsBook;
    private BookSelection _selection = BookSelection.None;

    private ElementReference _content;
    private ElementReference _searchInput;
    private IJSObjectReference? _module;
    private DotNetObjectReference<LibraryDetailPage>? _dotNetRef;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _sortCts;
    private readonly CancellationTokenSource _disposalCts = new();

    private string _searchTermValue = string.Empty;

    private string SearchTerm
    {
        get => _searchTermValue;
        set
        {
            _searchTermValue = value;
            var previous = _searchCts;
            previous?.Cancel();
            previous?.Dispose();
            _searchCts = new CancellationTokenSource();
            _ = ReloadOnSearchAsync(_searchCts.Token);
        }
    }

    // Covers or List: a library that opens on its series is sent to the series page.
    private LibraryView _currentViewMode = LibraryView.Covers;
    private BookSortOrder _currentSortOrder = BookSortOrder.IdDesc;

    private bool IsDigital => _library?.BookType == LibraryBookType.Digital;

    // Checked books being moved or deleted: neither can start again until it is done.
    private bool IsChangingBooks => _isMoving || _isDeleting;

    // The library on screen: the side bar links to the other libraries, which are shown in this same page.
    private string? _shownLibraryId;

    // This page as it is shown: back from a book comes here (#1261).
    private string HereHref => "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

    // Created before any script call: a render that shows the books before the browser answered the first call
    // (back from a book) must observe the sentinel with it (#1230).
    protected override void OnInitialized() => _dotNetRef = DotNetObjectReference.Create(this);

    protected override async Task OnParametersSetAsync()
    {
        if (_shownLibraryId == LibraryId)
        {
            return;
        }

        if (_shownLibraryId is not null)
        {
            await LeaveLibraryAsync();
        }

        _shownLibraryId = LibraryId;
        await LoadDataAsync();
    }

    // Keeps the search of the library left, then forgets everything shown of it.
    private async Task LeaveLibraryAsync()
    {
        LibraryStateService.Save(_libraryGuid, new LibraryPageState(SearchTerm));

        if (_searchCts is not null)
        {
            await _searchCts.CancelAsync();
            _searchCts.Dispose();
            _searchCts = null;
        }
        if (_sortCts is not null)
        {
            await _sortCts.CancelAsync();
            _sortCts.Dispose();
            _sortCts = null;
        }

        // A page of the library left that is still loading is dropped.
        _loadVersion++;
        _library = null;
        _displayedBooks.Clear();
        _currentPage = 1;
        _hasNextPage = false;
        _libraryBookCount = null;
        _resultCount = 0;
        _shownSearch = string.Empty;
        _searchTermValue = string.Empty;
        _isSearchOpen = false;
        _currentViewMode = LibraryView.Covers;
        _observerInitialized = false;
        _openSheet = LibrarySheet.None;
        _actionsBook = null;
        _selection = BookSelection.None;
        await ScrollToTopAsync();
    }

    private async Task ScrollToTopAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("scrollToTop", _disposalCts.Token, _content);
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to scroll library {LibraryId} back to the top", _libraryGuid);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("bodyScroll.disable", _disposalCts.Token);
                var module = await JS.InvokeAsync<IJSObjectReference?>("import", _disposalCts.Token, AppVersion.Versioned("./js/libraryPage.js"));
                if (module is not null)
                {
                    await module.InvokeVoidAsync("attach", _disposalCts.Token, _content, _dotNetRef);
                    _module = module;
                }
            }

            if (_focusSearch)
            {
                await _searchInput.FocusAsync();
                _focusSearch = false;
            }

            if (!_observerInitialized && !_isLoading && _displayedBooks.Count > 0)
            {
                // Observed again after each page: a sentinel still on screen asks for the next one.
                await JS.InvokeVoidAsync("infiniteScroll.observe", _disposalCts.Token, _dotNetRef, "scroll-sentinel", ".library-detail-content");
                _observerInitialized = true;
            }
        }
        catch (OperationCanceledException)
        {
            // Component disposed mid-render; nothing to update.
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to set up the scripts of library {LibraryId}", _libraryGuid);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        if (_searchCts is not null)
        {
            await _searchCts.CancelAsync();
            _searchCts.Dispose();
        }
        if (_sortCts is not null)
        {
            await _sortCts.CancelAsync();
            _sortCts.Dispose();
        }
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
        try
        {
            await JS.InvokeVoidAsync("bodyScroll.enable", CancellationToken.None);
            await JS.InvokeVoidAsync("infiniteScroll.dispose", CancellationToken.None);
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("detach", CancellationToken.None, _content);
                await _module.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // JS interop can fail when the Blazor circuit is disconnected or during static prerendering; intentionally ignored.
        }
        finally
        {
            LibraryStateService.Save(_libraryGuid, new LibraryPageState(SearchTerm));
            _dotNetRef?.Dispose();
        }
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;

        try
        {
            if (!Guid.TryParse(LibraryId, out _libraryGuid))
            {
                NavigationManager.NavigateTo("/libraries/list");
                return;
            }

            var saved = LibraryStateService.Load(_libraryGuid);
            if (saved is not null)
            {
                _searchTermValue = saved.Search;
                _isSearchOpen = !string.IsNullOrEmpty(saved.Search);
            }

            var libResult = await LibrariesService.GetById(LibraryId);
            if (libResult.IsFailure)
            {
                Snackbar.Add("Library not found", Severity.Error);
                NavigationManager.NavigateTo("/libraries/list");
                return;
            }

            _library = LibraryUiDto.Convert(libResult.Value!);
            _currentSortOrder = _library.DefaultBookSortOrder;

            if (_library.DefaultView == LibraryView.Series)
            {
                NavigationManager.NavigateTo(SerieDisplay.SeriesHref(_libraryGuid), replace: true);
                return;
            }
            _currentViewMode = _library.DefaultView == LibraryView.List ? LibraryView.List : LibraryView.Covers;

            await LoadFirstPageAsync(_disposalCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Component disposed while loading; nothing to update.
        }
        finally
        {
            _isLoading = false;
        }
    }

    // ── Loading ────────────────────────────────────────────────────────

    // Replaces the books on screen by the first page of the current sort and search.
    private async Task LoadFirstPageAsync(CancellationToken cancellationToken)
    {
        var version = ++_loadVersion;
        var sortOrder = _currentSortOrder;
        var search = SearchTerm;

        var result = await BooksService.GetPagedByLibrary(_libraryGuid, 1, PageSize, sortOrder, search, cancellationToken);

        // A newer load, sort or search started meanwhile: its own results will be shown.
        if (version != _loadVersion || sortOrder != _currentSortOrder || search != SearchTerm)
        {
            return;
        }

        if (result.IsSuccess && result.Value?.Items is not null)
        {
            _displayedBooks.Clear();
            _displayedBooks.AddRange(result.Value.Items.Select(BookListItemViewModel.From));
            _currentPage = 1;
            _hasNextPage = result.Value.HasNextPage;
            _resultCount = result.Value.TotalCount;
            _shownSearch = search;
            if (string.IsNullOrWhiteSpace(search))
            {
                _libraryBookCount = result.Value.TotalCount;
            }
            _observerInitialized = false;
        }
        else if (result.IsFailure)
        {
            Snackbar.Add("Failed to load books", Severity.Error);
            Logger.LogError("Failed to load books for library {LibraryId}: {ErrorDescription}", _libraryGuid, result.Error!.Description);
        }
    }

    [JSInvokable]
    public async Task LoadMoreBooksAsync()
    {
        if (!_hasNextPage || _isLoadingMore)
        {
            return;
        }

        _isLoadingMore = true;
        await InvokeAsync(StateHasChanged);

        var version = _loadVersion;
        var nextPage = _currentPage + 1;
        var cancelled = false;

        try
        {
            var result = await BooksService.GetPagedByLibrary(
                _libraryGuid, nextPage, PageSize, _currentSortOrder, _shownSearch, _disposalCts.Token);

            if (version == _loadVersion)
            {
                if (result.IsSuccess && result.Value?.Items is not null)
                {
                    _displayedBooks.AddRange(result.Value.Items.Select(BookListItemViewModel.From));
                    _hasNextPage = result.Value.HasNextPage;
                    _currentPage = nextPage;
                    _observerInitialized = false;
                }
                else if (result.IsFailure)
                {
                    Snackbar.Add("Failed to load books", Severity.Error);
                    Logger.LogError("Failed to load books for library {LibraryId}: {ErrorDescription}", _libraryGuid, result.Error!.Description);
                }
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        finally
        {
            _isLoadingMore = false;
            if (!cancelled)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    // ── Search ─────────────────────────────────────────────────────────

    private async Task ReloadOnSearchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(300, cancellationToken);
            await InvokeAsync(async () =>
            {
                await LoadFirstPageAsync(cancellationToken);
                StateHasChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // A newer search was typed: this one is dropped.
        }
    }

    // On a phone the search takes the place of the bar.
    private void OpenSearch()
    {
        _isSearchOpen = true;
        _focusSearch = true;
    }

    private void CloseSearch()
    {
        _isSearchOpen = false;
        ClearSearch();
    }

    private void ClearSearch()
    {
        if (!string.IsNullOrEmpty(SearchTerm))
        {
            SearchTerm = string.Empty;
        }
    }

    // ── Sort and view ──────────────────────────────────────────────────

    private Task OpenSortAndViewAsync() => OpenSheetAsync(LibrarySheet.SortAndView, SortAndViewButtonId);

    private Task OpenMoreActionsAsync() => OpenSheetAsync(LibrarySheet.MoreActions, MoreActionsButtonId);

    private async Task ChooseSortOrderAsync(BookSortOrder sortOrder)
    {
        CloseSheet();
        if (sortOrder != _currentSortOrder)
        {
            await SetSortOrderAsync(sortOrder);
        }
    }

    private async Task SetSortOrderAsync(BookSortOrder sortOrder)
    {
        if (_library is null)
        {
            return;
        }

        var previous = _sortCts;
        if (previous is not null)
        {
            await previous.CancelAsync();
            previous.Dispose();
        }
        _sortCts = CancellationTokenSource.CreateLinkedTokenSource(_disposalCts.Token);
        var token = _sortCts.Token;

        try
        {
            var result = await LibrariesService.Update(new UpdateLibraryRequest(
                _library.Id.ToString(),
                null,
                _library.Color,
                _library.Icon,
                sortOrder), token);

            if (result.IsSuccess)
            {
                _library.DefaultBookSortOrder = sortOrder;
                _currentSortOrder = sortOrder;
                await LoadFirstPageAsync(token);
            }
            else
            {
                Snackbar.Add("Failed to save sort order", Severity.Error);
                Logger.LogError("Failed to save sort order for library {LibraryId}: {ErrorDescription}", _library.Id, result.Error!.Description);
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer sort request, or component disposed; nothing to update.
        }
    }

    // The chosen view becomes the one the library opens with (#1210); Series leaves for the series page.
    private async Task ChooseViewAsync(LibraryView view)
    {
        CloseSheet();
        if (_library is null || (view != _library.DefaultView && !await SaveDefaultViewAsync(_library, view)))
        {
            return;
        }

        if (view == LibraryView.Series)
        {
            NavigationManager.NavigateTo(SerieDisplay.SeriesHref(_libraryGuid));
        }
        else if (_currentViewMode != view)
        {
            _currentViewMode = view;
            // The sentinel is rendered again under the new view.
            _observerInitialized = false;
        }
    }

    private async Task<bool> SaveDefaultViewAsync(LibraryUiDto library, LibraryView view)
    {
        try
        {
            var result = await LibrariesService.Update(new UpdateLibraryRequest(
                library.Id.ToString(),
                null,
                library.Color,
                library.Icon,
                DefaultView: view), _disposalCts.Token);

            if (result.IsSuccess)
            {
                library.DefaultView = view;
                return true;
            }

            Snackbar.Add("Failed to save the view", Severity.Error);
            Logger.LogError("Failed to save the view of library {LibraryId}: {ErrorDescription}", library.Id, result.Error!.Description);
        }
        catch (OperationCanceledException)
        {
            // Component disposed while saving; nothing to update.
        }
        return false;
    }

    // ── Sheets ─────────────────────────────────────────────────────────

    private async Task OpenSheetAsync(LibrarySheet sheet, string anchorElementId)
    {
        var anchor = await AnchorOfAsync(anchorElementId);
        _sheetAnchor = anchor;
        _sheetAlignEnd = true;
        _openSheet = sheet;
    }

    internal async Task OpenBookActionsAsync(BookListItemViewModel book)
    {
        var anchor = await AnchorOfAsync(BookActionsIds.Of(book.Id));
        ShowBookActions(book, anchor, alignEnd: true);
    }

    // Long press or right click on a book (libraryPage.js): the menu opens at the finger.
    [JSInvokable]
    public void OpenBookActionsAt(string bookId, PopupAnchor anchor)
    {
        var book = Guid.TryParse(bookId, out var id) ? _displayedBooks.Find(b => b.Id == id) : null;
        if (book is null)
        {
            return;
        }

        // While books are chosen, a long press checks the book like a tap.
        if (_selection.IsActive)
        {
            ToggleBook(book);
        }
        else
        {
            ShowBookActions(book, anchor, alignEnd: false);
        }
        StateHasChanged();
    }

    private void ShowBookActions(BookListItemViewModel book, PopupAnchor? anchor, bool alignEnd)
    {
        _actionsBook = book;
        _sheetAnchor = anchor;
        _sheetAlignEnd = alignEnd;
        _openSheet = LibrarySheet.BookActions;
    }

    private void CloseSheet() => _openSheet = LibrarySheet.None;

    // Without the script (or before it is loaded) the menu is centered on a tablet.
    private async Task<PopupAnchor?> AnchorOfAsync(string elementId)
    {
        if (_module is null)
        {
            return null;
        }

        try
        {
            return await _module.InvokeAsync<PopupAnchor?>("anchorOf", _disposalCts.Token, elementId);
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to find the position of {ElementId}", elementId);
            return null;
        }
    }

    // ── Actions ────────────────────────────────────────────────────────

    private void GoBack() => NavigationManager.NavigateTo("/libraries/list");

    // The labelled button on a tablet, the + on a phone (the other one is hidden, without a size).
    private async Task OpenAddBookAsync()
    {
        var anchor = await AnchorOfAsync(AddBookButtonId);
        if (anchor is null || anchor.Right <= anchor.Left)
        {
            anchor = await AnchorOfAsync(AddBookIconId);
        }

        _sheetAnchor = anchor;
        _sheetAlignEnd = true;
        _openSheet = LibrarySheet.AddBook;
    }

    private void NavigateToImport() => NavigationManager.NavigateTo($"/import?libraryId={LibraryId}");

    private async Task FindMissingIsbnsAsync()
    {
        if (_library is null)
        {
            return;
        }

        CloseSheet();
        _isStartingIsbnScan = true;
        try
        {
            var result = await IsbnScanService.StartLibraryScanAsync(_library.Id, CancellationToken.None);
            if (result.IsFailure)
            {
                Snackbar.Add("Unable to start the search of the missing ISBNs", Severity.Error);
                Logger.LogError("Unable to start the ISBN scan of library {LibraryId}: {Error}", _library.Id, result.Error?.Code);
            }
            else if (result.Value == 0)
            {
                Snackbar.Add("Every book of this library has an ISBN or was already scanned", Severity.Info);
            }
            else
            {
                Snackbar.Add($"Reading the pages of {result.Value} book(s) in the background. A book showing several ISBNs lets you pick its own.", Severity.Success);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            Snackbar.Add("Unable to start the search of the missing ISBNs", Severity.Error);
            Logger.LogError(ex, "Unexpected error while starting the ISBN scan of library {LibraryId}", _library.Id);
        }
        finally
        {
            _isStartingIsbnScan = false;
        }
    }

    // A book left the library (moved or deleted): one book less, counted again by a load without search.
    private Task ReloadBooksAsync() => ReloadBooksAsync(1);

    private async Task ReloadBooksAsync(int booksGone)
    {
        if (_libraryBookCount is { } count)
        {
            _libraryBookCount = Math.Max(count - booksGone, 0);
        }
        await LoadFirstPageAsync(_disposalCts.Token);
    }

    // ── Several books (#1150) ──────────────────────────────────────────

    // From the bar, or from the actions of a book, which is then the first one checked.
    private void StartSelecting(Guid? bookId = null)
    {
        CloseSheet();
        _selection = BookSelection.Start(bookId);
    }

    private void StopSelecting() => _selection = BookSelection.None;

    private void ToggleBook(BookListItemViewModel book) => _selection = _selection.Toggle(book.Id);

    // The books loaded so far: those further down are loaded by scrolling.
    private void CheckEveryShownBook() => _selection = _selection.With(_displayedBooks.Select(b => b.Id));

    internal async Task MoveCheckedBooksAsync()
    {
        if (IsChangingBooks || _selection.Count == 0)
        {
            return;
        }

        _isMoving = true;
        try
        {
            var moved = await BookMoveWorkflow.ChooseAndMoveAsync(_selection.BookIds, _disposalCts.Token);
            if (moved > 0)
            {
                // The books have left this library: the ones left are shown, none checked.
                _selection = BookSelection.None;
                await ReloadBooksAsync(moved);
            }
        }
        catch (OperationCanceledException)
        {
            // Component disposed while moving; nothing to update.
        }
        finally
        {
            _isMoving = false;
        }
    }

    internal async Task DeleteCheckedBooksAsync()
    {
        if (IsChangingBooks || _selection.Count == 0)
        {
            return;
        }

        var bookIds = _selection.BookIds;
        var confirmed = await DialogService.ShowConfirmationAsync(
            BookDeleteText.Title(bookIds.Count), BookDeleteText.Confirmation(bookIds.Count, IsDigital), BookDeleteText.Title(bookIds.Count));
        if (!confirmed)
        {
            return;
        }

        _isDeleting = true;
        try
        {
            var result = await BooksService.Delete(bookIds, _disposalCts.Token);
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error!.Description ?? "Failed to delete the books", Severity.Error);
                Logger.LogError("Failed to delete {Count} books: {Description}", bookIds.Count, result.Error.Description);
                return;
            }

            var deleted = result.Value!;
            Snackbar.Add(BookDeleteText.Outcome(deleted), BookDeleteText.Severity(deleted));
            if (deleted.Deleted > 0)
            {
                // The books are gone: the ones left are shown, none checked.
                _selection = BookSelection.None;
                await ReloadBooksAsync(deleted.Deleted);
            }
        }
        catch (OperationCanceledException)
        {
            // Component disposed while deleting; nothing to update.
        }
        finally
        {
            _isDeleting = false;
        }
    }
}
