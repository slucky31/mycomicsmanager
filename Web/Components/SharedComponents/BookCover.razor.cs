using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

// A book in a grid of covers (#1265): the same cover, title and ⋮ in a library, a series and the books in progress.
public partial class BookCover
{
    [Parameter, EditorRequired]
    public Guid BookId { get; set; }

    // Names the book in the alt text of its cover and in the labels of its buttons.
    [Parameter, EditorRequired]
    public string Title { get; set; } = default!;

    // The text under the cover when it is not the title.
    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public string? ImageLink { get; set; }

    // The volume, at the bottom left of the cover.
    [Parameter]
    public string? Badge { get; set; }

    [Parameter]
    public string? Href { get; set; }

    // What the link says when it does more than open the book.
    [Parameter]
    public string? LinkLabel { get; set; }

    // Shown on the cover, over its bottom right.
    [Parameter]
    public RenderFragment? CoverContent { get; set; }

    // Shown between the cover and the title.
    [Parameter]
    public RenderFragment? BelowCover { get; set; }

    // The ⋮ of the book; left out without it.
    [Parameter]
    public EventCallback OnActions { get; set; }

    // The books being chosen, to move them at once (#1150).
    [Parameter]
    public bool Selecting { get; set; }

    [Parameter]
    public bool Checked { get; set; }

    [Parameter]
    public EventCallback OnToggle { get; set; }
}
