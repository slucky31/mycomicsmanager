using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages;

public partial class Page
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string Icon { get; set; } = string.Empty;
}
