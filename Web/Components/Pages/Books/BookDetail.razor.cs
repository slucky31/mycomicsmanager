using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Components.Pages.Dialogs;
using Web.Services;

namespace Web.Components.Pages.Books;

public partial class BookDetail
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IBooksService BooksService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public string BookId { get; set; } = string.Empty;

    private Book? _book;
    private bool _isLoading = true;
    private bool _loadError;
    private bool _showAddReading;
    private int _newRating = 1;
    private bool _isAddingReading;

    protected override async Task OnInitializedAsync()
    {
        await LoadBookAsync();
    }

    private async Task LoadBookAsync()
    {
        _isLoading = true;
        _loadError = false;
        StateHasChanged();

        try
        {
            var result = await BooksService.GetById(BookId);

            if (result.IsSuccess && result.Value is not null)
            {
                _book = result.Value;
            }
            else
            {
                _loadError = true;
            }
        }
        catch
        {
            _loadError = true;
        }

        _isLoading = false;
    }

    private async Task AddReadingDateAsync()
    {
        _isAddingReading = true;
        StateHasChanged();

        try
        {
            var result = await BooksService.AddReadingDate(BookId, _newRating);

            if (result.IsSuccess)
            {
                Snackbar.Add("Lecture ajoutée.", Severity.Success);
                _showAddReading = false;
                _newRating = 1;
                await LoadBookAsync();
            }
            else
            {
                Snackbar.Add($"Erreur : {result.Error?.Description}", Severity.Error);
            }
        }
        finally
        {
            _isAddingReading = false;
        }
    }

    private void EditBook()
    {
        NavigationManager.NavigateTo($"/books/{BookId}/edit");
    }

    private async Task DeleteBookAsync()
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
            var res = await BooksService.Delete(BookId);

            if (res.IsSuccess)
            {
                Snackbar.Add("Book deleted successfully!", Severity.Success);
                NavigationManager.NavigateTo("/books/list");
            }
            else
            {
                Snackbar.Add($"Failed to delete book: {res.Error?.Description}", Severity.Error);
            }
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/books/list");
    }
}
