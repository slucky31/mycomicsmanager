using Domain.Books;
using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages.Libraries.Views;

public partial class BooksCoversView
{
    [Parameter, EditorRequired]
    public IReadOnlyList<Book> Books { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback<Guid> OnDelete { get; set; }
}
