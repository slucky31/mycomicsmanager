using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.Pages.Libraries.Views;

// Covers view: 3 columns on a phone, 6 on a tablet and a large screen.
public partial class BooksCoversView
{
    [Parameter, EditorRequired]
    public IReadOnlyList<BookListItemViewModel> Books { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback<BookListItemViewModel> OnActions { get; set; }

    // The books being chosen, to move them at once (#1150).
    [Parameter]
    public BookSelection Selection { get; set; } = BookSelection.None;

    [Parameter]
    public EventCallback<BookListItemViewModel> OnToggle { get; set; }

    // The page the books are shown on: back from a book comes here (#1261).
    [Parameter]
    public string? From { get; set; }
}
