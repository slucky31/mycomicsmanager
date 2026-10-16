using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class FormHeader
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

    // The actions of the form, shown in the bar from the tablet on (on a phone they are in a FormFooter).
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
