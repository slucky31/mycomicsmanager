using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages;

public partial class Page
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public String Title { get; set; } = string.Empty;

    [Parameter]
    public String Icon { get; set; } = string.Empty;
}
