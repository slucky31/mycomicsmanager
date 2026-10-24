using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Extensions;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Libraries;

public sealed partial class LibrariesList : IDisposable
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private PopupAnchorLocator PopupAnchorLocator { get; set; } = default!;
    [Inject] private ILogger<LibrariesList> Logger { get; set; } = default!;

    public static readonly string Msg_LibCorrectlyDeleted = "The library was correctly deleted";
    public static readonly string Msg_LoadFailed = "Failed to load the libraries";
    public static readonly string Msg_DeleteFailed = "Failed to delete library";
    public static readonly string Msg_SearchFailed = "Failed to search the libraries";

    // Books shown at first, then added by "Show more" (#1209).
    public const int SearchPageSize = 20;

    private readonly CancellationTokenSource _disposalCts = new();
    private List<LibraryCardViewModel> _libraries = [];
    private bool _isLoading = true;
    private LibraryCardViewModel? _actionsLibrary;
    private PopupAnchor? _sheetAnchor;

    // The search of all the libraries (#1209): the typed term is searched after a pause.
    private CancellationTokenSource? _searchCts;
    private string _shownSearch = string.Empty;
    private LibrariesSearchViewModel _searchResults = LibrariesSearchViewModel.Empty;
    private int _bookLimit = SearchPageSize;
    private bool _isSearchLoading;
    // Bumped on each search: an older answer arriving late is dropped.
    private int _searchVersion;

    private bool IsSearching => !string.IsNullOrWhiteSpace(SearchTerm);

    private string SearchTerm
    {
        get;
        set
        {
            field = value;
            var previous = _searchCts;
            previous?.Cancel();
            previous?.Dispose();
            _searchCts = new CancellationTokenSource();
            if (string.IsNullOrWhiteSpace(value))
            {
                ForgetSearch();
                return;
            }
            _ = SearchAfterPauseAsync(value.Trim(), _searchCts.Token);
        }
    } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadLibrariesAsync();
        _isLoading = false;
    }

    private async Task LoadLibrariesAsync()
    {
        var result = await LibrariesService.GetSummaries(_disposalCts.Token);
        if (result.IsSuccess && result.Value is not null)
        {
            _libraries = [.. result.Value.Select(LibraryCardViewModel.From)];
        }
        else if (result.IsFailure)
        {
            Logger.LogError("Failed to load the libraries: {Error}", result.Error?.Description);
            Snackbar.Add(Msg_LoadFailed, Severity.Error);
        }
    }

    private async Task SearchAfterPauseAsync(string term, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(300, cancellationToken);
            await InvokeAsync(async () =>
            {
                await SearchAsync(term, SearchPageSize, cancellationToken);
                StateHasChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // A newer search was typed: this one is dropped.
        }
    }

    private async Task SearchAsync(string term, int bookLimit, CancellationToken cancellationToken)
    {
        var version = ++_searchVersion;
        _isSearchLoading = true;
        try
        {
            var result = await BooksService.Search(term, bookLimit, cancellationToken);
            if (version != _searchVersion)
            {
                return;
            }

            if (result.IsSuccess && result.Value is not null)
            {
                _searchResults = LibrariesSearchViewModel.From(result.Value, _libraries);
                _shownSearch = term;
                _bookLimit = bookLimit;
            }
            else if (result.IsFailure)
            {
                Logger.LogError("Failed to search the libraries: {Error}", result.Error?.Description);
                Snackbar.Add(Msg_SearchFailed, Severity.Error);
            }
        }
        finally
        {
            if (version == _searchVersion)
            {
                _isSearchLoading = false;
            }
        }
    }

    private Task ShowMoreAsync() => SearchAsync(_shownSearch, _bookLimit + SearchPageSize, _disposalCts.Token);

    private void ClearSearch() => SearchTerm = string.Empty;

    private void ForgetSearch()
    {
        _searchVersion++;
        _isSearchLoading = false;
        _shownSearch = string.Empty;
        _searchResults = LibrariesSearchViewModel.Empty;
        _bookLimit = SearchPageSize;
    }

    private async Task OpenActionsAsync(LibraryCardViewModel library)
    {
        var anchor = await PopupAnchorLocator.AnchorOfAsync(library.ActionsButtonId, _disposalCts.Token);
        _sheetAnchor = anchor;
        _actionsLibrary = library;
    }

    private void CloseActions() => _actionsLibrary = null;

    private async Task DeleteAsync(LibraryCardViewModel library)
    {
        CloseActions();

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Delete library",
            $"The library “{library.Name}” will be deleted with its {LibraryDisplay.BookCount(library.BookCount)}. This cannot be undone.",
            "Delete library");
        if (!confirmed)
        {
            return;
        }

        var result = await LibrariesService.Delete(library.Id.ToString(), _disposalCts.Token);
        if (result.IsFailure)
        {
            Logger.LogError("Failed to delete the library {LibraryId}: {Error}", library.Id, result.Error?.Description);
            Snackbar.Add(Msg_DeleteFailed, Severity.Error);
            return;
        }

        Snackbar.Add(Msg_LibCorrectlyDeleted, Severity.Success);
        await LoadLibrariesAsync();
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _disposalCts.Cancel();
        _disposalCts.Dispose();
    }
}
