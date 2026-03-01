using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;
using Web.Components.Pages.Dialogs;
using Web.Enums;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Libraries;

public partial class LibraryDetailPage
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

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

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
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
        _filteredBooks = string.IsNullOrWhiteSpace(_searchTerm)
            ? _books
            : _books.Where(b =>
                b.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(b.Serie) && b.Serie.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)))
              .ToList();
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
