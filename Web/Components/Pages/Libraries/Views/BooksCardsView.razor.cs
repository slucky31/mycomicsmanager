using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.Pages.Libraries.Views;

public partial class BooksCardsView
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReadOnlyList<BookListItemViewModel> Books { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback<Guid> OnDelete { get; set; }

    [Parameter]
    public bool ShowDownload { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDownload { get; set; }

    private void NavigateToBook(Guid id) => NavigationManager.NavigateTo($"/books/{id}");
}
