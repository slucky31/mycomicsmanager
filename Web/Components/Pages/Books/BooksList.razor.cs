using Domain.Books;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Components.Pages.Dialogs;
using Web.Services;

namespace Web.Components.Pages.Books;

public enum ViewMode
{
    Cards,
    Covers,
    List
}

public partial class BooksList
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private IBooksService BooksService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private List<Book> Books { get; set; } = [];

    private ViewMode CurrentViewMode { get; set; } = ViewMode.Cards;

    protected override async Task OnInitializedAsync()
    {
        await LoadBooksAsync();
    }

    private async Task LoadBooksAsync()
    {
        var result = await BooksService.GetAll();
        if (result.IsSuccess && result.Value is not null)
        {
            Books = result.Value;
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("Failed to load books", Severity.Error);
        }
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
                Snackbar.Add("Book deleted successfully", Severity.Success);
                await LoadBooksAsync();
            }
            else
            {
                Snackbar.Add($"Failed to delete book: {res.Error?.Description}", Severity.Error);
            }
        }
    }
}
