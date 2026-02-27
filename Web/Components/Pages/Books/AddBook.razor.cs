using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages.Books;

public partial class AddBook
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? LibraryId { get; set; }

    private void GoToScan()
    {
        var url = string.IsNullOrEmpty(LibraryId) ? "/books/add/scan" : $"/books/add/scan?libraryId={LibraryId}";
        NavigationManager.NavigateTo(url);
    }

    private void GoToManual()
    {
        var url = string.IsNullOrEmpty(LibraryId) ? "/books/add/manual" : $"/books/add/manual?libraryId={LibraryId}";
        NavigationManager.NavigateTo(url);
    }

    private void GoToForm()
    {
        var url = string.IsNullOrEmpty(LibraryId) ? "/books/add/form" : $"/books/add/form?libraryId={LibraryId}";
        NavigationManager.NavigateTo(url);
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/libraries/list");
    }
}
