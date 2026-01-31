using Domain.Books;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Components.Pages.Dialogs;
using Web.Enums;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages.Books;

public partial class BooksList
{

    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private IBooksService BooksService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private List<Book> Books { get; set; } = [];

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

    private async Task CreateOrEditAsync(FormMode formMode, Book? book)
    {

        var parameters = new DialogParameters<BookDialog>
        {
            { x => x.FormMode, formMode },
            { x => x.Book, book },
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            CloseButton = false
        };

        var dialog = await DialogService.ShowAsync<BookDialog>("Book", parameters, options);
        var result = await dialog.Result;

        if (result is not null && result.Data is not null && !result.Canceled)
        {
            var b = (BookUiDto)result.Data;
            if (formMode == FormMode.Create)
            {
                var res = await BooksService.Create(
                    b.Serie,
                    b.Title,
                    b.ISBN,
                    b.VolumeNumber,
                    b.ImageLink,
                    b.Rating
                );

                await DisplaySnackbarAsync(res.IsSuccess, "Book created successfully", $"Failed to create book: {res.Error?.Description}");
            }
            else if (formMode == FormMode.Edit)
            {
                var res = await BooksService.Update(
                    b.Id.ToString(),
                    b.Serie,
                    b.Title,
                    b.ISBN,
                    b.VolumeNumber,
                    b.ImageLink,
                    b.Rating
                );

                await DisplaySnackbarAsync(res.IsSuccess, "Book updated successfully", $"Failed to update book: {res.Error?.Description}");
            }
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
            await DisplaySnackbarAsync(res.IsSuccess, "Book deleted successfully", $"Failed to delete book: {res.Error?.Description}");

        }
    }

    private async Task DisplaySnackbarAsync(bool success, string successMessage, string failureMessage)
    {
        if (success)
        {
            Snackbar.Add(successMessage, Severity.Success);
            await LoadBooksAsync();
        }
        else
        {
            Snackbar.Add(failureMessage, Severity.Error);
        }
    }

}
