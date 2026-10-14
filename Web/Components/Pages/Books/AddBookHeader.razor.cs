using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages.Books;

public partial class AddBookHeader
{
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string? Subtitle { get; set; }

    // Back to this page; OnBack instead when going back stays on the page.
    [Parameter]
    public string BackHref { get; set; } = "/libraries/list";

    [Parameter]
    public EventCallback OnBack { get; set; }

    // Shown on a tablet only.
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
