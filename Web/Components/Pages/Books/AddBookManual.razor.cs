using Application.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Web.Components.Pages.Books;

public partial class AddBookManual
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private string _isbn = string.Empty;
    private bool _hasError;
    private string _errorMessage = string.Empty;

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_isbn))
        {
            SearchByIsbn();
        }
    }

    private void SearchByIsbn()
    {
        _hasError = false;
        _errorMessage = string.Empty;

        var normalizedIsbn = _isbn.Replace("-", "").Replace(" ", "").Trim();

        if (string.IsNullOrWhiteSpace(normalizedIsbn))
        {
            _hasError = true;
            _errorMessage = "Please enter an ISBN number.";
            return;
        }

        if (!IsbnHelper.IsValidISBN(normalizedIsbn))
        {
            _hasError = true;
            _errorMessage = "Invalid ISBN format. Please enter a valid 10 or 13 digit ISBN.";
            return;
        }

        NavigationManager.NavigateTo($"/books/add/form?isbn={Uri.EscapeDataString(normalizedIsbn)}");
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/books/add");
    }
}
