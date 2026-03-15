using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.Pages.Libraries.Views;

public partial class BooksListView
{
    [Parameter, EditorRequired]
    public IReadOnlyList<BookListItemViewModel> Books { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback<Guid> OnDelete { get; set; }
}
