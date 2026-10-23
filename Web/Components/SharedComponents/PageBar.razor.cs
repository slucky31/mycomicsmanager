using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class PageBar
{
    [Parameter, EditorRequired]
    public string BackHref { get; set; } = "/";

    [Parameter, EditorRequired]
    public string BackLabel { get; set; } = "Back";

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Subtitle { get; set; }

    // The actions at the end of the bar.
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
