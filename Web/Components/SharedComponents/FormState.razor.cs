using Microsoft.AspNetCore.Components;

namespace Web.Components.SharedComponents;

public partial class FormState
{
    // A spinner and the text; else the icon, the title and the text.
    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public string Icon { get; set; } = string.Empty;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Text { get; set; }

    // The way out, e.g. a button back to the list.
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
