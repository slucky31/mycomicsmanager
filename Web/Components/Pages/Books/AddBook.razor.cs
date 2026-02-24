using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages.Books;

public partial class AddBook
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private void GoToScan()
    {
        NavigationManager.NavigateTo("/books/add/scan");
    }

    private void GoToManual()
    {
        NavigationManager.NavigateTo("/books/add/manual");
    }

    private void GoToForm()
    {
        NavigationManager.NavigateTo("/books/add/form");
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/books/list");
    }
}
