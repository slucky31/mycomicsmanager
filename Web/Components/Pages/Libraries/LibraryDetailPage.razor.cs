using System.Globalization;
using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Serilog;
using Web.Components.Pages.Dialogs;
using Web.Components.Pages.Libraries.Views;
using Web.Enums;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class LibraryDetailPage : IAsyncDisposable
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private LibraryStateService LibraryStateService { get; set; } = default!;

    [Parameter] public string? LibraryId { get; set; }

    private const int PageSize = 24;

    private LibraryUiDto? _library;
    private Guid _libraryGuid;

    // Cards / Covers — accumulated pages
    private readonly List<BookListItemViewModel> _displayedBooks = [];
    private int _currentPage = 1;
    private bool _hasNextPage;
    private bool _isLoadingMore;

    // List view — component reference for reload
    private BooksListView? _booksListView;

    private bool _isLoading = true;
    private bool _observerInitialized;
    private DotNetObjectReference<LibraryDetailPage>? _dotNetRef;
    private CancellationTokenSource? _searchCts;

    private string _searchTermValue = string.Empty;

    private string _searchTerm
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

    private ViewMode _currentViewMode = ViewMode.Cards;
    private BookSortOrder _currentSortOrder = BookSortOrder.IdDesc;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("bodyScroll.disable");
            _dotNetRef = DotNetObjectReference.Create(this);
        }

        if (!_observerInitialized && !_isLoading
            && _currentViewMode != ViewMode.List
            && _displayedBooks.Count > 0)
        {
            await JS.InvokeVoidAsync("infiniteScroll.observe", _dotNetRef, "scroll-sentinel", ".library-detail-content");
            _observerInitialized = true;
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
        try
        {
            await JS.InvokeVoidAsync("bodyScroll.enable");
            await JS.InvokeVoidAsync("infiniteScroll.dispose");
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // JS interop can fail when the Blazor circuit is disconnected or during static prerendering; intentionally ignored.
        }
        finally
        {
            LibraryStateService.Save(_libraryGuid, new LibraryPageState(_searchTerm, _currentViewMode));
            _dotNetRef?.Dispose();
        }
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        _observerInitialized = false;

        if (!Guid.TryParse(LibraryId, out _libraryGuid))
        {
            NavigationManager.NavigateTo("/libraries/list");
            return;
        }

        var saved = LibraryStateService.Load(_libraryGuid);
        if (saved is not null)
        {
            _searchTermValue = saved.Search;
            _currentViewMode = saved.View;
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

        // Always load first page so we can detect an empty library
        _currentPage = 1;
        _displayedBooks.Clear();
        _hasNextPage = false;
        await LoadBooksPageAsync();

        _isLoading = false;
    }

    // ── Cards / Covers ─────────────────────────────────────────────────

    private async Task LoadBooksPageAsync(CancellationToken cancellationToken = default)
    {
        var result = await BooksService.GetPagedByLibrary(
            _libraryGuid, _currentPage, PageSize, _currentSortOrder, _searchTerm, cancellationToken);

        if (result.IsSuccess && result.Value?.Items is not null)
        {
            _displayedBooks.AddRange(result.Value.Items.Select(BookListItemViewModel.From));
            _hasNextPage = result.Value.HasNextPage;
        }
        else if (result.IsFailure)
        {
            Snackbar.Add("Failed to load books", Severity.Error);
            Log.Error("Failed to load books for library {LibraryId}: {ErrorDescription}", _libraryGuid, result.Error?.Description);
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

        var capturedLibrary = _libraryGuid;
        var capturedSort = _currentSortOrder;
        var capturedSearch = _searchTerm;
        var nextPage = _currentPage + 1;

        try
        {
            var result = await BooksService.GetPagedByLibrary(
                capturedLibrary, nextPage, PageSize, capturedSort, capturedSearch);

            if (_libraryGuid == capturedLibrary
                && _currentSortOrder == capturedSort
                && _searchTerm == capturedSearch)
            {
                if (result.IsSuccess && result.Value?.Items is not null)
                {
                    _displayedBooks.AddRange(result.Value.Items.Select(BookListItemViewModel.From));
                    _hasNextPage = result.Value.HasNextPage;
                    _currentPage = nextPage;
                }
                else if (result.IsFailure)
                {
                    Snackbar.Add("Failed to load books", Severity.Error);
                    Log.Error("Failed to load books for library {LibraryId}: {ErrorDescription}", _libraryGuid, result.Error?.Description);
                }
            }
        }
        finally
        {
            _isLoadingMore = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    // ── List view ───────────────────────────────────────────────────────

    public async Task<TableData<BookListItemViewModel>> LoadListDataAsync(TableState state, CancellationToken ct)
    {
        // MudTable pages are 0-based; our service is 1-based
        var result = await BooksService.GetPagedByLibrary(
            _libraryGuid, state.Page + 1, state.PageSize, _currentSortOrder, _searchTerm, ct);

        if (result.IsSuccess && result.Value?.Items is not null)
        {
            return new TableData<BookListItemViewModel>
            {
                Items = result.Value.Items.Select(BookListItemViewModel.From).ToList(),
                TotalItems = result.Value.TotalCount
            };
        }

        if (result.IsFailure)
        {
            Snackbar.Add("Failed to load books", Severity.Error);
            Log.Error("Failed to load books for library {LibraryId}: {ErrorDescription}", _libraryGuid, result.Error?.Description);
        }

        return new TableData<BookListItemViewModel> { Items = [], TotalItems = 0 };
    }

    // ── Shared: search / sort ──────────────────────────────────────────

    private async Task ReloadOnSearchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(300, cancellationToken);

            if (_currentViewMode == ViewMode.List)
            {
                await InvokeAsync(async () =>
                {
                    if (_booksListView is not null)
                    {
                        await _booksListView.ReloadAsync();
                    }
                    StateHasChanged();
                });
                return;
            }

            _currentPage = 1;
            _displayedBooks.Clear();
            _hasNextPage = false;
            _observerInitialized = false;
            await LoadBooksPageAsync(cancellationToken);
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
            // Nouvelle frappe reçue : abandon silencieux
        }
    }

    private async Task SetSortOrderAsync(BookSortOrder sortOrder)
    {
        if (_library is null)
        {
            return;
        }

        var result = await LibrariesService.Update(new UpdateLibraryRequest(
            _library.Id.ToString(),
            null,
            _library.Color,
            _library.Icon,
            sortOrder));

        if (result.IsSuccess)
        {
            _library.DefaultBookSortOrder = sortOrder;
            _currentSortOrder = sortOrder;

            if (_currentViewMode == ViewMode.List)
            {
                if (_booksListView is not null)
                {
                    await _booksListView.ReloadAsync();
                }
                return;
            }

            _currentPage = 1;
            _displayedBooks.Clear();
            _hasNextPage = false;
            _observerInitialized = false;
            await LoadBooksPageAsync();
        }
        else
        {
            Snackbar.Add("Failed to save sort order", Severity.Error);
            Log.Error("Failed to save sort order for library {LibraryId}: {ErrorDescription}", _library.Id, result.Error?.Description);
        }
    }

    private async Task SetViewModeAsync(ViewMode mode)
    {
        if (_currentViewMode == mode)
        {
            return;
        }

        // Switching away from Cards/Covers → disconnect observer
        if (_currentViewMode != ViewMode.List && mode == ViewMode.List)
        {
            _observerInitialized = false;
            await JS.InvokeVoidAsync("infiniteScroll.dispose");
        }
        // Switching back to Cards/Covers → let OnAfterRenderAsync re-init the observer
        else if (_currentViewMode == ViewMode.List)
        {
            _observerInitialized = false;
        }

        _currentViewMode = mode;
    }

    // ── Utilities ──────────────────────────────────────────────────────

    private string LibColorRgba(double alpha)
    {
        var hex = _library?.Color ?? "#5C6BC0";
        if (hex.StartsWith('#') && hex.Length == 7
            && int.TryParse(hex[1..3], NumberStyles.HexNumber, null, out var r)
            && int.TryParse(hex[3..5], NumberStyles.HexNumber, null, out var g)
            && int.TryParse(hex[5..7], NumberStyles.HexNumber, null, out var b))
        {
            return FormattableString.Invariant($"rgba({r},{g},{b},{alpha})");
        }
        return FormattableString.Invariant($"rgba(92,107,192,{alpha})");
    }

    private void GoBack() => NavigationManager.NavigateTo("/libraries/list");

    private void AddBook() => NavigationManager.NavigateTo($"/books/add?libraryId={LibraryId}");

    private void NavigateToImport() => NavigationManager.NavigateTo($"/import?libraryId={LibraryId}");

    private async Task DownloadBookAsync(Guid bookId)
    {
        await JS.InvokeVoidAsync("open", $"/api/books/{bookId}/download", "_blank");
    }

    private async Task DeleteAsync(Guid bookId)
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ConfirmationMessage, "Do you really want to delete this book? This process cannot be undone." },
            { x => x.ActionText, "Delete" },
            { x => x.ColorConfirmButton, Color.Error }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.ExtraSmall,
            CloseButton = false
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm Delete", parameters, options);
        var result = await dialog.Result;

        if (result is not null && result.Data is not null && !result.Canceled)
        {
            var res = await BooksService.Delete(bookId.ToString());

            if (res.IsSuccess)
            {
                await LoadDataAsync();
                if (_currentViewMode == ViewMode.List && _booksListView is not null)
                {
                    await _booksListView.ReloadAsync();
                }
            }
            else
            {
                Snackbar.Add("Failed to delete book", Severity.Error);
                Log.Error("Failed to delete book with ID {BookId}: {ErrorDescription}", bookId, res.Error?.Description);
            }
        }
    }
}
