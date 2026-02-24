using Microsoft.AspNetCore.Components;

namespace Web.Components.Pages;

public partial class PageWithAction
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Icon { get; set; }

    [Parameter]
    public string? ActionName { get; set; }

    [Parameter]
    public string? ActionIcon { get; set; }

    [Parameter]
    public EventCallback OnActionClick { get; set; }
}
