using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.Pages.Libraries.Views;

// List view: 1 column on a phone, 2 on a tablet, 3 on a large screen.
public partial class BooksListView
{
    [Parameter, EditorRequired]
    public IReadOnlyList<BookListItemViewModel> Books { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback<BookListItemViewModel> OnActions { get; set; }
}
