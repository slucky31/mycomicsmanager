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
}
