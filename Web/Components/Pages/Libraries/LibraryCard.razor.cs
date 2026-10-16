using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Components.Pages.Libraries;

public partial class LibraryCard
{
    [Parameter, EditorRequired]
    public LibraryCardViewModel Library { get; set; } = default!;

    // The page of the library; the card is not a link without it.
    [Parameter]
    public string? Href { get; set; }

    // Beside the link, e.g. the button of its actions.
    [Parameter]
    public RenderFragment? Actions { get; set; }
}
