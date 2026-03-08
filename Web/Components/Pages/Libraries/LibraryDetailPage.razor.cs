using Domain.Books;
using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Serilog;
using Web.Components.Pages.Dialogs;
using Web.Enums;
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

    [Parameter] public string? LibraryId { get; set; }

    private LibraryUiDto? _library;
    private List<Book> _books = [];
    private List<Book> _filteredBooks = [];
    private bool _isLoading = true;

    private string _searchTerm
    {
        get;
        set
        {
            field = value;
            UpdateFilteredBooks();
        }
    } = string.Empty;

    private ViewMode _currentViewMode = ViewMode.Cards;
    private BookSortOrder _currentSortOrder = BookSortOrder.IdDesc;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await JS.InvokeVoidAsync("bodyScroll.disable");
    }

    public async ValueTask DisposeAsync()
    {
        await JS.InvokeVoidAsync("bodyScroll.enable");
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;

        if (!Guid.TryParse(LibraryId, out var libraryGuid))
        {
            NavigationManager.NavigateTo("/libraries/list");
            return;
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

        var booksResult = await BooksService.GetByLibrary(libraryGuid);
        if (booksResult.IsSuccess && booksResult.Value is not null)
        {
            _books = booksResult.Value;
            UpdateFilteredBooks();
        }

        _isLoading = false;
    }

    private void UpdateFilteredBooks()
    {
        var filtered = string.IsNullOrWhiteSpace(_searchTerm)
            ? _books
            : _books.Where(b =>
                b.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(b.Serie) && b.Serie.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)));

        _filteredBooks = _currentSortOrder switch
        {
            BookSortOrder.IdAsc => filtered.OrderBy(b => b.Id).ToList(),
            BookSortOrder.SerieAndVolumeAsc => filtered
                .OrderBy(b => b.Serie, StringComparer.OrdinalIgnoreCase)
                .ThenBy(b => b.VolumeNumber)
                .ToList(),
            _ => filtered.OrderByDescending(b => b.Id).ToList()
        };
    }

    private void SetSortOrder(BookSortOrder sortOrder)
    {
        _currentSortOrder = sortOrder;
        UpdateFilteredBooks();
    }

    private string LibColorRgba(double alpha)
    {
        var hex = _library?.Color ?? "#5C6BC0";
        if (hex.StartsWith('#') && hex.Length == 7)
        {
            var r = Convert.ToInt32(hex[1..3], 16);
            var g = Convert.ToInt32(hex[3..5], 16);
            var b = Convert.ToInt32(hex[5..7], 16);
            return FormattableString.Invariant($"rgba({r},{g},{b},{alpha})");
        }
        return FormattableString.Invariant($"rgba(92,107,192,{alpha})");
    }

    private void GoBack() => NavigationManager.NavigateTo("/libraries/list");

    private void AddBook() => NavigationManager.NavigateTo($"/books/add?libraryId={LibraryId}");

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
            }
            else
            {
                Snackbar.Add("Failed to delete book", Severity.Error);
                Log.Error("Failed to delete book with ID {BookId}: {ErrorDescription}", bookId, res.Error?.Description);
            }
        }
    }
}
