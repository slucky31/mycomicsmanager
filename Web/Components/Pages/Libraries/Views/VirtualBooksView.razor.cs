using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.Pages.Libraries.Views;

public sealed partial class VirtualBooksView
{
    private static readonly IReadOnlyList<BookListItemViewModel> NoBooks = [];
    private static readonly Func<BookListItemViewModel, object> KeyOf = book => book.Id;

    // The books of the current sort and search; another instance is shown from the top, the same books again in place.
    [Parameter, EditorRequired]
    public LibraryBookRows Rows { get; set; } = default!;

    [Parameter]
    public LibraryView View { get; set; } = LibraryView.Covers;

    // The book to open the library on: the first one on screen when it was left.
    [Parameter]
    public int InitialBook { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<BookListItemViewModel> OnActions { get; set; }

    [Parameter]
    public BookSelection Selection { get; set; } = BookSelection.None;

    [Parameter]
    public EventCallback<BookListItemViewModel> OnToggle { get; set; }

    [Parameter]
    public string? From { get; set; }

    // The first book on screen, once the scroll stops.
    [Parameter]
    public EventCallback<int> OnPositionChanged { get; set; }

    // A guess of the height of a row, measured again by Virtualize once rendered.
    private float RowSize => View == LibraryView.List ? 112 : 230;
}
